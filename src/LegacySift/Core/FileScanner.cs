using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace LegacySift.Core
{
    internal static class FileScanner
    {
        public static List<FileRecord> EnumerateFilesSafe(
            string root,
            List<ScanError> errors,
            CancellationToken token,
            Action<ProgressInfo> progress,
            string phase)
        {
            var files = new List<FileRecord>();
            var stack = new Stack<string>();
            stack.Push(PathSafety.Normalize(root));
            int visitedDirs = 0;

            while (stack.Count > 0)
            {
                token.ThrowIfCancellationRequested();
                var current = stack.Pop();
                visitedDirs++;
                progress?.Invoke(new ProgressInfo { Phase = phase, Current = visitedDirs, Total = 0, CurrentPath = current });

                string[] childDirs;
                try { childDirs = Directory.GetDirectories(current); }
                catch (Exception ex)
                {
                    errors.Add(new ScanError { Path = current, Message = ex.Message });
                    childDirs = new string[0];
                }

                foreach (var dir in childDirs)
                {
                    token.ThrowIfCancellationRequested();
                    try
                    {
                        var attrs = File.GetAttributes(dir);
                        if ((attrs & FileAttributes.ReparsePoint) != 0)
                        {
                            errors.Add(new ScanError { Path = dir, Message = L10n.T("ReparseDir") });
                            continue;
                        }
                        stack.Push(dir);
                    }
                    catch (Exception ex)
                    {
                        errors.Add(new ScanError { Path = dir, Message = ex.Message });
                    }
                }

                string[] childFiles;
                try { childFiles = Directory.GetFiles(current); }
                catch (Exception ex)
                {
                    errors.Add(new ScanError { Path = current, Message = ex.Message });
                    childFiles = new string[0];
                }

                foreach (var file in childFiles)
                {
                    token.ThrowIfCancellationRequested();
                    try
                    {
                        var attrs = File.GetAttributes(file);
                        if ((attrs & FileAttributes.ReparsePoint) != 0)
                        {
                            errors.Add(new ScanError { Path = file, Message = L10n.T("ReparseFile") });
                            continue;
                        }

                        var info = new FileInfo(file);
                        files.Add(new FileRecord
                        {
                            FullPath = info.FullName,
                            RelativePath = PathSafety.GetRelativePath(root, info.FullName),
                            Name = info.Name,
                            Length = info.Length,
                            LastWriteUtc = info.LastWriteTimeUtc
                        });
                    }
                    catch (Exception ex)
                    {
                        errors.Add(new ScanError { Path = file, Message = ex.Message });
                    }
                }
            }

            return files;
        }
    }
}
