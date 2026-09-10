using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace LegacySift.Core
{
    internal sealed class ComparisonEngine
    {
        private readonly HashCache _hashCache = new HashCache();

        public AnalysisResult Analyze(string sourceRoot, string referenceRoot, CancellationToken token, Action<ProgressInfo> progress)
        {
            sourceRoot = PathSafety.Normalize(sourceRoot);
            referenceRoot = PathSafety.Normalize(referenceRoot);

            var result = new AnalysisResult { SourceRoot = sourceRoot, ReferenceRoot = referenceRoot };

            progress?.Invoke(new ProgressInfo { Phase = L10n.T("PhaseOldScan"), Current = 0, Total = 0 });
            var sourceFiles = FileScanner.EnumerateFilesSafe(sourceRoot, result.ScanErrors, token, progress, L10n.T("PhaseOldScan"));
            progress?.Invoke(new ProgressInfo { Phase = L10n.T("PhaseCurrentScan"), Current = 0, Total = 0 });
            var referenceFiles = FileScanner.EnumerateFilesSafe(referenceRoot, result.ScanErrors, token, progress, L10n.T("PhaseCurrentScan"));

            result.SourceFileCount = sourceFiles.Count;
            result.ReferenceFileCount = referenceFiles.Count;

            var byLength = new Dictionary<long, List<FileRecord>>();
            var byName = new Dictionary<string, List<FileRecord>>(StringComparer.OrdinalIgnoreCase);

            foreach (var file in referenceFiles)
            {
                List<FileRecord> list;
                if (!byLength.TryGetValue(file.Length, out list))
                {
                    list = new List<FileRecord>();
                    byLength[file.Length] = list;
                }
                list.Add(file);

                if (!byName.TryGetValue(file.Name, out list))
                {
                    list = new List<FileRecord>();
                    byName[file.Name] = list;
                }
                list.Add(file);
            }

            for (int i = 0; i < sourceFiles.Count; i++)
            {
                token.ThrowIfCancellationRequested();
                var source = sourceFiles[i];
                progress?.Invoke(new ProgressInfo
                {
                    Phase = L10n.T("PhaseCompare"),
                    Current = i + 1,
                    Total = sourceFiles.Count,
                    CurrentPath = source.RelativePath
                });

                try
                {
                    FileRecord exactReference = null;
                    string sourceHash = null;
                    List<FileRecord> sameLength;
                    if (byLength.TryGetValue(source.Length, out sameLength))
                    {
                        sourceHash = _hashCache.GetSha256(source.FullPath, token);
                        foreach (var candidate in sameLength)
                        {
                            token.ThrowIfCancellationRequested();
                            try
                            {
                                var refHash = _hashCache.GetSha256(candidate.FullPath, token);
                                if (StringComparer.OrdinalIgnoreCase.Equals(sourceHash, refHash))
                                {
                                    exactReference = candidate;
                                    break;
                                }
                            }
                            catch (Exception ex)
                            {
                                result.ScanErrors.Add(new ScanError { Path = candidate.FullPath, Message = "Reference hash: " + ex.Message });
                            }
                        }
                    }

                    if (exactReference != null)
                    {
                        result.Items.Add(new ComparisonItem
                        {
                            Kind = ComparisonKind.ExactDuplicate,
                            Source = source,
                            ReferencePath = exactReference.FullPath,
                            Sha256 = sourceHash,
                            Note = L10n.T("ExactNote")
                        });
                        continue;
                    }

                    List<FileRecord> sameName;
                    if (byName.TryGetValue(source.Name, out sameName) && sameName.Count > 0)
                    {
                        result.Items.Add(new ComparisonItem
                        {
                            Kind = ComparisonKind.PossibleVersion,
                            Source = source,
                            ReferencePath = sameName[0].FullPath,
                            Sha256 = sourceHash,
                            Note = sameName.Count == 1
                                ? L10n.T("VersionNoteOne")
                                : L10n.T("VersionNoteMany", sameName.Count)
                        });
                    }
                    else
                    {
                        result.Items.Add(new ComparisonItem
                        {
                            Kind = ComparisonKind.Unique,
                            Source = source,
                            Sha256 = sourceHash,
                            Note = L10n.T("UniqueNote")
                        });
                    }
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    result.Items.Add(new ComparisonItem
                    {
                        Kind = ComparisonKind.Error,
                        Source = source,
                        Note = ex.Message
                    });
                }
            }

            return result;
        }
    }
}
