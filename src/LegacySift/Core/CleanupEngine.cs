using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.VisualBasic.FileIO;

namespace LegacySift.Core
{
    internal sealed class CleanupEngine
    {
        public CleanupResult Clean(
            AnalysisResult analysis,
            CleanupMode mode,
            bool removeEmptyFolders,
            CancellationToken token,
            Action<ProgressInfo> progress)
        {
            if (analysis == null) throw new ArgumentNullException(nameof(analysis));

            var pairError = PathSafety.ValidatePair(analysis.SourceRoot, analysis.ReferenceRoot);
            if (pairError != null) throw new InvalidOperationException(pairError);

            var exactItems = analysis.Items.FindAll(x => x.Kind == ComparisonKind.ExactDuplicate);
            var result = new CleanupResult { Mode = mode };

            string quarantineRoot = null;
            if (mode == CleanupMode.Quarantine)
            {
                var parent = Directory.GetParent(analysis.SourceRoot);
                if (parent == null) throw new InvalidOperationException(L10n.T("SafetyParentMissing"));
                var name = new DirectoryInfo(analysis.SourceRoot).Name;
                quarantineRoot = Path.Combine(parent.FullName, name + "__LegacySift_Safety_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"));
                Directory.CreateDirectory(quarantineRoot);
                result.QuarantineRoot = quarantineRoot;
                result.SessionFile = WriteSessionFile(quarantineRoot, analysis.SourceRoot, analysis.ReferenceRoot);
            }

            for (int i = 0; i < exactItems.Count; i++)
            {
                token.ThrowIfCancellationRequested();
                var item = exactItems[i];
                progress?.Invoke(new ProgressInfo
                {
                    Phase = mode == CleanupMode.Quarantine ? L10n.T("PhaseSafety") : L10n.T("PhaseRecycle"),
                    Current = i + 1,
                    Total = exactItems.Count,
                    CurrentPath = item.Source?.RelativePath
                });

                var row = new CleanupItemResult
                {
                    SourcePath = item.Source?.FullPath,
                    ReferencePath = item.ReferencePath
                };

                try
                {
                    if (item.Source == null || string.IsNullOrEmpty(item.Source.FullPath) || !File.Exists(item.Source.FullPath))
                        throw new IOException(L10n.T("OldFileMissingNow"));
                    if (string.IsNullOrEmpty(item.ReferencePath) || !File.Exists(item.ReferencePath))
                        throw new IOException(L10n.T("CurrentCopyMissingNow"));

                    var sourceInfo = new FileInfo(item.Source.FullPath);
                    var referenceInfo = new FileInfo(item.ReferencePath);
                    if (sourceInfo.Length != referenceInfo.Length)
                        throw new IOException(L10n.T("ChangedSize"));

                    var sourceHash = HashCache.ComputeSha256(item.Source.FullPath, token);
                    var referenceHash = HashCache.ComputeSha256(item.ReferencePath, token);
                    if (!StringComparer.OrdinalIgnoreCase.Equals(sourceHash, referenceHash))
                        throw new IOException(L10n.T("ChangedHash"));

                    if (mode == CleanupMode.Quarantine)
                    {
                        var relative = PathSafety.GetRelativePath(analysis.SourceRoot, item.Source.FullPath);
                        var destination = Path.Combine(quarantineRoot, relative);
                        var destinationDir = Path.GetDirectoryName(destination);
                        if (!string.IsNullOrEmpty(destinationDir)) Directory.CreateDirectory(destinationDir);
                        if (File.Exists(destination))
                            throw new IOException(L10n.T("SafetyDestinationExists"));

                        File.Move(item.Source.FullPath, destination);
                        row.DestinationPath = destination;

                        var movedHash = HashCache.ComputeSha256(destination, CancellationToken.None);
                        if (!StringComparer.OrdinalIgnoreCase.Equals(sourceHash, movedHash))
                        {
                            try
                            {
                                if (!File.Exists(item.Source.FullPath))
                                {
                                    var originalDir = Path.GetDirectoryName(item.Source.FullPath);
                                    if (!string.IsNullOrEmpty(originalDir)) Directory.CreateDirectory(originalDir);
                                    File.Move(destination, item.Source.FullPath);
                                }
                            }
                            catch { }
                            throw new IOException(L10n.T("SafetyVerificationFailed"));
                        }

                        row.Success = true;
                        row.Message = L10n.T("MovedSafety");
                    }
                    else
                    {
                        FileSystem.DeleteFile(
                            item.Source.FullPath,
                            UIOption.OnlyErrorDialogs,
                            RecycleOption.SendToRecycleBin,
                            UICancelOption.ThrowException);
                        row.Success = true;
                        row.Message = L10n.T("MovedRecycle");
                    }

                    result.RemovedCount++;
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    row.Success = false;
                    row.Message = ex.Message;
                    result.SkippedCount++;
                    result.ErrorCount++;
                }

                result.Items.Add(row);
            }

            if (removeEmptyFolders)
                RemoveEmptyDirectories(analysis.SourceRoot);

            if (mode == CleanupMode.Quarantine && !string.IsNullOrEmpty(quarantineRoot))
                WriteCleanupLog(quarantineRoot, result);

            return result;
        }

        public RestoreResult RestoreQuarantine(string quarantineRoot, CancellationToken token, Action<ProgressInfo> progress)
        {
            var result = new RestoreResult();
            if (string.IsNullOrWhiteSpace(quarantineRoot) || !Directory.Exists(quarantineRoot))
                throw new DirectoryNotFoundException(L10n.T("SafetyFolderMissing"));

            string sourceRoot;
            string referenceRoot;
            ReadSessionFile(quarantineRoot, out sourceRoot, out referenceRoot);

            if (!Directory.Exists(sourceRoot)) Directory.CreateDirectory(sourceRoot);
            var errors = new List<ScanError>();
            var files = FileScanner.EnumerateFilesSafe(quarantineRoot, errors, token, progress, L10n.T("PhaseRestoreRead"));
            files.RemoveAll(x => StringComparer.OrdinalIgnoreCase.Equals(x.RelativePath, "LegacySift.session") || StringComparer.OrdinalIgnoreCase.Equals(x.RelativePath, "cleanup-log.csv"));

            for (int i = 0; i < files.Count; i++)
            {
                token.ThrowIfCancellationRequested();
                var file = files[i];
                progress?.Invoke(new ProgressInfo { Phase = L10n.T("PhaseRestore"), Current = i + 1, Total = files.Count, CurrentPath = file.RelativePath });
                try
                {
                    var dest = Path.Combine(sourceRoot, file.RelativePath);
                    if (File.Exists(dest))
                    {
                        result.ConflictCount++;
                        result.Messages.Add(L10n.T("RestoreNoOverwrite", dest));
                        continue;
                    }
                    var dir = Path.GetDirectoryName(dest);
                    if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
                    File.Move(file.FullPath, dest);
                    result.RestoredCount++;
                }
                catch (Exception ex)
                {
                    result.ErrorCount++;
                    result.Messages.Add(file.FullPath + " -> " + ex.Message);
                }
            }

            if (result.ConflictCount == 0 && result.ErrorCount == 0)
            {
                try { File.Delete(Path.Combine(quarantineRoot, "LegacySift.session")); } catch { }
                try { File.Delete(Path.Combine(quarantineRoot, "cleanup-log.csv")); } catch { }
            }
            RemoveEmptyDirectories(quarantineRoot, deleteRootIfEmpty: result.ConflictCount == 0 && result.ErrorCount == 0);
            return result;
        }

        private static string WriteSessionFile(string quarantineRoot, string sourceRoot, string referenceRoot)
        {
            var path = Path.Combine(quarantineRoot, "LegacySift.session");
            var lines = new[]
            {
                "LegacySiftSession=1",
                "CreatedUtc=" + DateTime.UtcNow.ToString("o"),
                "SourceRootBase64=" + ToBase64(sourceRoot),
                "ReferenceRootBase64=" + ToBase64(referenceRoot)
            };
            File.WriteAllLines(path, lines, Encoding.UTF8);
            return path;
        }

        private static void ReadSessionFile(string quarantineRoot, out string sourceRoot, out string referenceRoot)
        {
            var path = Path.Combine(quarantineRoot, "LegacySift.session");
            if (!File.Exists(path)) throw new InvalidDataException(L10n.T("SessionMissing"));
            sourceRoot = null;
            referenceRoot = null;
            foreach (var line in File.ReadAllLines(path, Encoding.UTF8))
            {
                if (line.StartsWith("SourceRootBase64=", StringComparison.Ordinal))
                    sourceRoot = FromBase64(line.Substring("SourceRootBase64=".Length));
                else if (line.StartsWith("ReferenceRootBase64=", StringComparison.Ordinal))
                    referenceRoot = FromBase64(line.Substring("ReferenceRootBase64=".Length));
            }
            if (string.IsNullOrWhiteSpace(sourceRoot)) throw new InvalidDataException(L10n.T("SessionInvalid"));
        }

        private static string ToBase64(string value) => Convert.ToBase64String(Encoding.UTF8.GetBytes(value ?? string.Empty));
        private static string FromBase64(string value) => Encoding.UTF8.GetString(Convert.FromBase64String(value));

        private static void WriteCleanupLog(string quarantineRoot, CleanupResult result)
        {
            var path = Path.Combine(quarantineRoot, "cleanup-log.csv");
            using (var writer = new StreamWriter(path, false, new UTF8Encoding(true)))
            {
                writer.WriteLine("Success;Source;Reference;Destination;Message");
                foreach (var item in result.Items)
                {
                    writer.WriteLine(
                        Csv(item.Success ? "YES" : "NO") + ";" +
                        Csv(item.SourcePath) + ";" +
                        Csv(item.ReferencePath) + ";" +
                        Csv(item.DestinationPath) + ";" +
                        Csv(item.Message));
                }
            }
        }

        private static string Csv(string value)
        {
            value = value ?? string.Empty;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }

        private static void RemoveEmptyDirectories(string root, bool deleteRootIfEmpty = false)
        {
            if (!Directory.Exists(root)) return;

            var discovered = new List<string>();
            var stack = new Stack<string>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                var current = stack.Pop();
                string[] children;
                try { children = Directory.GetDirectories(current); }
                catch { continue; }

                foreach (var child in children)
                {
                    try
                    {
                        var attrs = File.GetAttributes(child);
                        if ((attrs & FileAttributes.ReparsePoint) != 0) continue;
                        discovered.Add(child);
                        stack.Push(child);
                    }
                    catch { }
                }
            }

            discovered.Sort((a, b) => b.Length.CompareTo(a.Length));
            foreach (var dir in discovered)
            {
                try
                {
                    if (Directory.GetFileSystemEntries(dir).Length == 0) Directory.Delete(dir);
                }
                catch { }
            }

            if (deleteRootIfEmpty)
            {
                try
                {
                    if (Directory.Exists(root) && Directory.GetFileSystemEntries(root).Length == 0) Directory.Delete(root);
                }
                catch { }
            }
        }
    }
}
