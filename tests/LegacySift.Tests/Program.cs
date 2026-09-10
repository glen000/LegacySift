using LegacySift.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace LegacySift.Tests
{
    internal static class Program
    {
        private static int _assertions;

        private static int Main()
        {
            L10n.SetLanguage(AppLanguage.English);
            var root = Path.Combine(Path.GetTempPath(), "LegacySiftTests_" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(root);
                TestAnalyzeCleanupAndRestore(root);
                TestRestoreDoesNotOverwrite(root);
                TestPathSafety(root);
                TestCriticalWording();
                TestAllTranslations();
                Console.WriteLine("PASS — " + _assertions + " assertions");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("FAIL: " + ex);
                return 1;
            }
            finally
            {
                try { if (Directory.Exists(root)) Directory.Delete(root, true); } catch { }
            }
        }

        private static void TestAnalyzeCleanupAndRestore(string tempRoot)
        {
            var caseRoot = Path.Combine(tempRoot, "case1");
            var source = Path.Combine(caseRoot, "OLD");
            var reference = Path.Combine(caseRoot, "CURRENT");
            Directory.CreateDirectory(source);
            Directory.CreateDirectory(reference);
            Directory.CreateDirectory(Path.Combine(source, "sub"));
            Directory.CreateDirectory(Path.Combine(reference, "elsewhere"));

            Write(Path.Combine(source, "sub", "old-name.txt"), "same-content");
            Write(Path.Combine(reference, "elsewhere", "new-name.txt"), "same-content");
            Write(Path.Combine(source, "contract.docx"), "old-version");
            Write(Path.Combine(reference, "contract.docx"), "new-version");
            Write(Path.Combine(source, "only-old.txt"), "only-in-old");
            File.WriteAllBytes(Path.Combine(source, "zero-old.bin"), new byte[0]);
            File.WriteAllBytes(Path.Combine(reference, "zero-current.bin"), new byte[0]);

            var protectedFile = Path.Combine(reference, "elsewhere", "new-name.txt");
            var protectedBytesBefore = File.ReadAllBytes(protectedFile);
            var protectedWriteBefore = File.GetLastWriteTimeUtc(protectedFile);

            var engine = new ComparisonEngine();
            var analysis = engine.Analyze(source, reference, CancellationToken.None, null);

            Assert(analysis.ExactDuplicateCount == 2, "must find 2 exact duplicates");
            Assert(analysis.PossibleVersionCount == 1, "must find 1 possible different version");
            Assert(analysis.UniqueCount == 1, "must find 1 file to keep");
            Assert(File.Exists(protectedFile), "protected reference file must exist after analysis");

            var cleanup = new CleanupEngine().Clean(analysis, CleanupMode.Quarantine, true, CancellationToken.None, null);
            Assert(cleanup.RemovedCount == 2, "must move 2 exact duplicates to the safety folder");
            Assert(!File.Exists(Path.Combine(source, "sub", "old-name.txt")), "exact duplicate must leave OLD");
            Assert(!File.Exists(Path.Combine(source, "zero-old.bin")), "zero-byte exact duplicate must leave OLD");
            Assert(File.Exists(Path.Combine(source, "contract.docx")), "different version must remain in OLD");
            Assert(File.Exists(Path.Combine(source, "only-old.txt")), "unique file must remain in OLD");
            Assert(File.Exists(protectedFile), "CURRENT must not be modified by cleanup");
            Assert(BytesEqual(protectedBytesBefore, File.ReadAllBytes(protectedFile)), "CURRENT content must remain byte-identical");
            Assert(File.GetLastWriteTimeUtc(protectedFile) == protectedWriteBefore, "CURRENT timestamp must remain unchanged");
            Assert(!string.IsNullOrEmpty(cleanup.QuarantineRoot) && Directory.Exists(cleanup.QuarantineRoot), "safety folder must exist");
            Assert(cleanup.QuarantineRoot.Contains("__LegacySift_Safety_"), "safety folder should use a recognizable name");

            var restore = new CleanupEngine().RestoreQuarantine(cleanup.QuarantineRoot, CancellationToken.None, null);
            Assert(restore.RestoredCount == 2, "must restore 2 files");
            Assert(File.Exists(Path.Combine(source, "sub", "old-name.txt")), "file must return to original path");
            Assert(File.Exists(Path.Combine(source, "zero-old.bin")), "zero-byte file must return to original path");
            Assert(File.Exists(protectedFile), "CURRENT must still be intact after restore");
            Assert(BytesEqual(protectedBytesBefore, File.ReadAllBytes(protectedFile)), "CURRENT content must remain unchanged after restore");
        }

        private static void TestRestoreDoesNotOverwrite(string tempRoot)
        {
            var caseRoot = Path.Combine(tempRoot, "case2");
            var source = Path.Combine(caseRoot, "OLD");
            var reference = Path.Combine(caseRoot, "CURRENT");
            Directory.CreateDirectory(source);
            Directory.CreateDirectory(reference);

            Write(Path.Combine(source, "duplicate.txt"), "identical");
            Write(Path.Combine(reference, "copy.txt"), "identical");

            var analysis = new ComparisonEngine().Analyze(source, reference, CancellationToken.None, null);
            var cleanup = new CleanupEngine().Clean(analysis, CleanupMode.Quarantine, false, CancellationToken.None, null);
            Assert(cleanup.RemovedCount == 1, "setup cleanup should move one file");

            Write(Path.Combine(source, "duplicate.txt"), "new-file-created-after-cleanup");
            var restore = new CleanupEngine().RestoreQuarantine(cleanup.QuarantineRoot, CancellationToken.None, null);
            Assert(restore.RestoredCount == 0, "restore must not overwrite an existing file");
            Assert(restore.ConflictCount == 1, "restore must report one conflict");
            Assert(File.ReadAllText(Path.Combine(source, "duplicate.txt"), Encoding.UTF8).Contains("new-file-created-after-cleanup"), "existing file must remain untouched");
        }

        private static void TestPathSafety(string tempRoot)
        {
            var caseRoot = Path.Combine(tempRoot, "case3");
            var a = Path.Combine(caseRoot, "OLD");
            var b = Path.Combine(caseRoot, "CURRENT");
            var nested = Path.Combine(a, "nested");
            Directory.CreateDirectory(a);
            Directory.CreateDirectory(b);
            Directory.CreateDirectory(nested);

            Assert(PathSafety.ValidatePair(a, b) == null, "two separate folders must be valid");
            Assert(PathSafety.ValidatePair(a, a) != null, "same folder must be blocked");
            Assert(PathSafety.ValidatePair(a, nested) != null, "nested folders must be blocked");
            Assert(PathSafety.ValidatePair(nested, a) != null, "reverse nested folders must be blocked");
            Assert(PathSafety.GetRelativePath(a, Path.Combine(a, "nested", "x.txt")) == Path.Combine("nested", "x.txt"), "relative path must remain inside OLD");

            bool outsideBlocked = false;
            try { PathSafety.GetRelativePath(a, Path.Combine(b, "outside.txt")); }
            catch (InvalidOperationException) { outsideBlocked = true; }
            Assert(outsideBlocked, "cleanup path helper must reject a file outside OLD");
        }

        private static void TestCriticalWording()
        {
            L10n.SetLanguage(AppLanguage.English);
            Assert(L10n.T("TabRecover").Contains("CURRENT"), "English result label must explain where the file was not found");
            Assert(L10n.T("TabDuplicates").Contains("IDENTICAL"), "English duplicate label must say identical copies");
            Assert(L10n.T("CleanupExplanation", "1", "2", "3", "4").Contains("OLD"), "English cleanup explanation must say what remains in OLD");

            L10n.SetLanguage(AppLanguage.Italian);
            Assert(L10n.T("TabRecover").Contains("ATTUALE"), "Italian result label must explain where the file was not found");
            Assert(L10n.T("TabDuplicates").Contains("IDENTICHE"), "Italian duplicate label must say identical copies");
            Assert(L10n.T("CleanupExplanation", "1", "2", "3", "4").Contains("VECCHIA"), "Italian cleanup explanation must say what remains in VECCHIA");

            L10n.SetLanguage(AppLanguage.English);
        }

        private static void TestAllTranslations()
        {
            var english = L10n.TranslationForTests(AppLanguage.English);
            Assert(LanguageCatalog.All.Count == Enum.GetValues(typeof(AppLanguage)).Length, "language catalog must contain every AppLanguage value");
            Assert(english.Count >= 100, "English baseline should contain the complete UI dictionary");

            foreach (var info in LanguageCatalog.All)
            {
                var dict = L10n.TranslationForTests(info.Language);
                Assert(dict != null, info.Code + " dictionary must exist");
                Assert(dict.Count == english.Count, info.Code + " dictionary must have the same number of keys as English");
                Assert(L10n.FromCode(L10n.ToCode(info.Language)) == info.Language, info.Code + " language code must round-trip");
                Assert(!string.IsNullOrWhiteSpace(info.NativeName), info.Code + " must have a native language name");
                Assert(!string.IsNullOrWhiteSpace(info.DisplayName) && info.DisplayName.Contains("[" + info.Code + "]"), info.Code + " display name must always contain a visible language code");

                foreach (var pair in english)
                {
                    string translated;
                    Assert(dict.TryGetValue(pair.Key, out translated), info.Code + " is missing key " + pair.Key);
                    Assert(!string.IsNullOrWhiteSpace(translated), info.Code + " has an empty value for " + pair.Key);
                    Assert(PlaceholderSet(pair.Value).SetEquals(PlaceholderSet(translated)), info.Code + " placeholder mismatch for " + pair.Key);
                }
            }

            L10n.SetLanguage(AppLanguage.English);
        }

        private static HashSet<string> PlaceholderSet(string value)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            foreach (Match match in Regex.Matches(value ?? string.Empty, @"\{\d+\}"))
                result.Add(match.Value);
            return result;
        }

        private static void Write(string path, string content)
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
            File.WriteAllText(path, content, Encoding.UTF8);
        }

        private static bool BytesEqual(byte[] a, byte[] b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a == null || b == null || a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++) if (a[i] != b[i]) return false;
            return true;
        }

        private static void Assert(bool condition, string message)
        {
            _assertions++;
            if (!condition) throw new InvalidOperationException("Assertion failed: " + message);
        }
    }
}
