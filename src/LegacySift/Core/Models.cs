using System;
using System.Collections.Generic;

namespace LegacySift.Core
{
    internal enum ComparisonKind
    {
        ExactDuplicate,
        Unique,
        PossibleVersion,
        Error
    }

    internal enum CleanupMode
    {
        Quarantine,
        RecycleBin
    }

    internal sealed class FileRecord
    {
        public string FullPath { get; set; }
        public string RelativePath { get; set; }
        public string Name { get; set; }
        public long Length { get; set; }
        public DateTime LastWriteUtc { get; set; }
    }

    internal sealed class ComparisonItem
    {
        public ComparisonKind Kind { get; set; }
        public FileRecord Source { get; set; }
        public string ReferencePath { get; set; }
        public string Sha256 { get; set; }
        public string Note { get; set; }
    }

    internal sealed class ScanError
    {
        public string Path { get; set; }
        public string Message { get; set; }
    }

    internal sealed class AnalysisResult
    {
        public string SourceRoot { get; set; }
        public string ReferenceRoot { get; set; }
        public int SourceFileCount { get; set; }
        public int ReferenceFileCount { get; set; }
        public List<ComparisonItem> Items { get; set; } = new List<ComparisonItem>();
        public List<ScanError> ScanErrors { get; set; } = new List<ScanError>();

        public int ExactDuplicateCount => Items.FindAll(x => x.Kind == ComparisonKind.ExactDuplicate).Count;
        public int UniqueCount => Items.FindAll(x => x.Kind == ComparisonKind.Unique).Count;
        public int PossibleVersionCount => Items.FindAll(x => x.Kind == ComparisonKind.PossibleVersion).Count;
        public int ErrorCount => Items.FindAll(x => x.Kind == ComparisonKind.Error).Count + ScanErrors.Count;

        public long DuplicateBytes
        {
            get
            {
                long total = 0;
                foreach (var item in Items)
                {
                    if (item.Kind == ComparisonKind.ExactDuplicate && item.Source != null)
                        total += item.Source.Length;
                }
                return total;
            }
        }
    }

    internal sealed class ProgressInfo
    {
        public string Phase { get; set; }
        public int Current { get; set; }
        public int Total { get; set; }
        public string CurrentPath { get; set; }
    }

    internal sealed class CleanupItemResult
    {
        public string SourcePath { get; set; }
        public string ReferencePath { get; set; }
        public string DestinationPath { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
    }

    internal sealed class CleanupResult
    {
        public CleanupMode Mode { get; set; }
        public string QuarantineRoot { get; set; }
        public string SessionFile { get; set; }
        public List<CleanupItemResult> Items { get; set; } = new List<CleanupItemResult>();
        public int RemovedCount { get; set; }
        public int SkippedCount { get; set; }
        public int ErrorCount { get; set; }
    }

    internal sealed class RestoreResult
    {
        public int RestoredCount { get; set; }
        public int ConflictCount { get; set; }
        public int ErrorCount { get; set; }
        public List<string> Messages { get; set; } = new List<string>();
    }
}
