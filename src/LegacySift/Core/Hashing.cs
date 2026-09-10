using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading;

namespace LegacySift.Core
{
    internal sealed class HashCache
    {
        private sealed class Entry
        {
            public long Length;
            public DateTime LastWriteUtc;
            public string Hash;
        }

        private readonly Dictionary<string, Entry> _cache = new Dictionary<string, Entry>(StringComparer.OrdinalIgnoreCase);

        public string GetSha256(string path, CancellationToken token)
        {
            var info = new FileInfo(path);
            Entry entry;
            if (_cache.TryGetValue(path, out entry) && entry.Length == info.Length && entry.LastWriteUtc == info.LastWriteTimeUtc)
                return entry.Hash;

            var hash = ComputeSha256(path, token);
            _cache[path] = new Entry { Length = info.Length, LastWriteUtc = info.LastWriteTimeUtc, Hash = hash };
            return hash;
        }

        public static string ComputeSha256(string path, CancellationToken token)
        {
            using (var sha = SHA256.Create())
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 1024 * 1024, FileOptions.SequentialScan))
            {
                var buffer = new byte[1024 * 1024];
                int read;
                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    token.ThrowIfCancellationRequested();
                    sha.TransformBlock(buffer, 0, read, buffer, 0);
                }
                sha.TransformFinalBlock(new byte[0], 0, 0);
                return BitConverter.ToString(sha.Hash).Replace("-", string.Empty).ToLowerInvariant();
            }
        }
    }
}
