using LegacySift.Core;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;

namespace LegacySift.Tests
{
    internal static class Program
    {
        private static int _assertions;

        [STAThread]
        private static int Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
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
                TestCultureMapping();
                TestSavedLanguageSettings();
                TestSavedThemeSettings();
                TestThemePalettes();
                TestApplicationIcon();
                TestScriptCoverage();
                TestLayoutMatrix();
                TestLanguageDialogLayout();
                TestThemeDialogLayout();
                if (args.Length == 2 && args[0] == "--capture-layout")
                    CaptureRepresentativeLayouts(args[1]);
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
            Assert(LanguageCatalog.All.Count == 34, "the 0.2.3-alpha catalog must preserve exactly 34 languages");
            Assert(english.Count == 121, "English baseline must contain the 116 frozen workflow keys plus 5 theme keys");
            Assert(LanguageCatalog.All.Select(x => x.Flag).Distinct().Count() == 34, "every supported language must have one cataloged flag");
            Assert(LanguageCatalog.All[0].Language == AppLanguage.English && LanguageCatalog.All[1].Language == AppLanguage.Italian, "English and Italian must remain first for language recovery");
            Assert(LanguageCatalog.All.Skip(2).Select(x => x.EnglishName).SequenceEqual(LanguageCatalog.All.Skip(2).Select(x => x.EnglishName).OrderBy(x => x, StringComparer.Ordinal)), "remaining languages must have a stable English-name alphabetical order");

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

        private static void TestCultureMapping()
        {
            var expected = new Dictionary<string, AppLanguage>(StringComparer.OrdinalIgnoreCase)
            {
                { "en-US", AppLanguage.English }, { "it-IT", AppLanguage.Italian }, { "de-DE", AppLanguage.German },
                { "fr-FR", AppLanguage.French }, { "es-ES", AppLanguage.Spanish }, { "pt-BR", AppLanguage.Portuguese },
                { "pl-PL", AppLanguage.Polish }, { "nl-NL", AppLanguage.Dutch }, { "tr-TR", AppLanguage.Turkish },
                { "uk-UA", AppLanguage.Ukrainian }, { "zh-CN", AppLanguage.ChineseSimplified }, { "zh-TW", AppLanguage.ChineseSimplified },
                { "ja-JP", AppLanguage.Japanese }, { "hi-IN", AppLanguage.Hindi }, { "ro-RO", AppLanguage.Romanian },
                { "cs-CZ", AppLanguage.Czech }, { "el-GR", AppLanguage.Greek }, { "hu-HU", AppLanguage.Hungarian },
                { "sv-SE", AppLanguage.Swedish }, { "ko-KR", AppLanguage.Korean }, { "id-ID", AppLanguage.Indonesian },
                { "vi-VN", AppLanguage.Vietnamese }, { "da-DK", AppLanguage.Danish }, { "nb-NO", AppLanguage.NorwegianBokmal },
                { "no-NO", AppLanguage.NorwegianBokmal }, { "nn-NO", AppLanguage.NorwegianBokmal }, { "fi-FI", AppLanguage.Finnish },
                { "sk-SK", AppLanguage.Slovak }, { "bg-BG", AppLanguage.Bulgarian }, { "hr-HR", AppLanguage.Croatian },
                { "bn-BD", AppLanguage.Bengali }, { "th-TH", AppLanguage.Thai }, { "ms-MY", AppLanguage.Malay },
                { "fil-PH", AppLanguage.Filipino }, { "et-EE", AppLanguage.Estonian }, { "lv-LV", AppLanguage.Latvian },
                { "lt-LT", AppLanguage.Lithuanian }, { "ru-RU", AppLanguage.English }, { "ar-SA", AppLanguage.English }
            };
            foreach (var pair in expected)
                Assert(L10n.DetectLanguage(CultureInfo.GetCultureInfo(pair.Key)) == pair.Value, pair.Key + " Windows culture mapping");
        }

        private static void TestSavedLanguageSettings()
        {
            foreach (var info in LanguageCatalog.All)
            {
                var serialized = SettingsStore.SerializeLanguageSetting(info.Language);
                Assert(SettingsStore.ParseLanguageSetting(new[] { serialized.Trim() }, AppLanguage.English) == info.Language, info.Code + " setting must round-trip");
            }
            Assert(SettingsStore.ParseLanguageSetting(new[] { "language=ru" }, AppLanguage.Italian) == AppLanguage.English, "removed Russian setting must fall back to English");
            Assert(SettingsStore.ParseLanguageSetting(new[] { "language=unknown" }, AppLanguage.Italian) == AppLanguage.English, "unknown setting must fall back to English");
            Assert(SettingsStore.ParseLanguageSetting(new[] { "broken=true" }, AppLanguage.Italian) == AppLanguage.Italian, "unrelated setting line must preserve caller fallback");
            Assert(L10n.FromCode("in") == AppLanguage.Indonesian, "legacy Indonesian code must remain readable");
            Assert(L10n.FromCode("no") == AppLanguage.NorwegianBokmal, "generic Norwegian code must use Bokmål");
            Assert(L10n.FromCode("nn-NO") == AppLanguage.NorwegianBokmal, "Nynorsk setting must safely use the single Norwegian Bokmål UI");
        }

        private static void TestSavedThemeSettings()
        {
            foreach (ThemeMode mode in Enum.GetValues(typeof(ThemeMode)))
            {
                var serialized = SettingsStore.SerializeSettings(AppLanguage.Italian, mode);
                var lines = serialized.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
                Assert(SettingsStore.ParseLanguageSetting(lines, AppLanguage.English) == AppLanguage.Italian, mode + " settings must preserve the selected language");
                Assert(SettingsStore.ParseThemeSetting(lines, ThemeMode.Light) == mode, mode + " theme setting must round-trip");
            }

            Assert(SettingsStore.ParseThemeSetting(new[] { "theme=unknown" }, ThemeMode.Dark) == ThemeMode.Dark, "unknown theme setting must preserve caller fallback");
            Assert(SettingsStore.ParseThemeSetting(new[] { "broken=true" }, ThemeMode.Light) == ThemeMode.Light, "unrelated setting line must preserve theme fallback");
            Assert(SettingsStore.ParseThemeSetting(null, ThemeMode.System) == ThemeMode.System, "missing settings must preserve system theme fallback");
        }

        private static void TestThemePalettes()
        {
            var light = ThemeManager.ResolvePalette(ThemeMode.Light);
            var dark = ThemeManager.ResolvePalette(ThemeMode.Dark);
            Assert(!light.IsDark && dark.IsDark, "explicit light and dark modes must resolve deterministically");
            Assert(light.AppBackground != dark.AppBackground, "light and dark application surfaces must differ");
            Assert(light.Surface != dark.Surface, "light and dark card surfaces must differ");
            Assert(light.OldSurface != light.CurrentSurface, "light OLD and CURRENT surfaces must remain distinguishable");
            Assert(dark.OldSurface != dark.CurrentSurface, "dark OLD and CURRENT surfaces must remain distinguishable");
            Assert(ContrastRatio(light.Text, light.Surface) >= 4.5, "light primary text must meet WCAG AA contrast on cards");
            Assert(ContrastRatio(light.SecondaryText, light.Surface) >= 4.5, "light secondary text must meet WCAG AA contrast on cards");
            Assert(ContrastRatio(dark.Text, dark.Surface) >= 4.5, "dark primary text must meet WCAG AA contrast on cards");
            Assert(ContrastRatio(dark.SecondaryText, dark.Surface) >= 4.5, "dark secondary text must meet WCAG AA contrast on cards");
            Assert(ContrastRatio(light.SelectionText, light.Selection) >= 4.5, "light selected grid text must meet WCAG AA contrast");
            Assert(ContrastRatio(dark.SelectionText, dark.Selection) >= 4.5, "dark selected grid text must meet WCAG AA contrast");
            Assert(ContrastRatio(light.DisabledText, light.SecondarySurface) >= 3.0, "light disabled text must remain distinguishable");
            Assert(ContrastRatio(dark.DisabledText, dark.SecondarySurface) >= 3.0, "dark disabled text must remain distinguishable");
        }

        private static double ContrastRatio(Color foreground, Color background)
        {
            var lighter = Math.Max(RelativeLuminance(foreground), RelativeLuminance(background));
            var darker = Math.Min(RelativeLuminance(foreground), RelativeLuminance(background));
            return (lighter + 0.05) / (darker + 0.05);
        }

        private static double RelativeLuminance(Color color)
        {
            Func<byte, double> channel = value =>
            {
                var normalized = value / 255.0;
                return normalized <= 0.03928 ? normalized / 12.92 : Math.Pow((normalized + 0.055) / 1.055, 2.4);
            };
            return 0.2126 * channel(color.R) + 0.7152 * channel(color.G) + 0.0722 * channel(color.B);
        }

        private static void TestApplicationIcon()
        {
            using (var stream = typeof(MainForm).Assembly.GetManifestResourceStream(AppIcon.ResourceName))
                Assert(stream != null && stream.Length > 0, "application icon must be embedded with its stable resource name");

            using (var icon = AppIcon.CreateIcon())
            using (var bitmap = AppIcon.CreateBitmap(24))
            {
                Assert(icon != null, "application icon must be loadable by WinForms");
                Assert(bitmap.Width == 24 && bitmap.Height == 24, "application icon must render at the compact header size");
            }

            var iconPath = FindRepositoryFile(Path.Combine("src", "LegacySift", "Assets", "legacysift-icon.ico"));
            var sizes = new HashSet<int>();
            using (var reader = new BinaryReader(File.OpenRead(iconPath)))
            {
                Assert(reader.ReadUInt16() == 0, "ICO reserved header must be zero");
                Assert(reader.ReadUInt16() == 1, "application asset must be an ICO container");
                var count = reader.ReadUInt16();
                Assert(count >= 7, "application ICO must contain at least seven size entries");
                for (var i = 0; i < count; i++)
                {
                    var width = reader.ReadByte();
                    var height = reader.ReadByte();
                    reader.ReadBytes(14);
                    var resolvedWidth = width == 0 ? 256 : width;
                    var resolvedHeight = height == 0 ? 256 : height;
                    Assert(resolvedWidth == resolvedHeight, "every application icon frame must be square");
                    sizes.Add(resolvedWidth);
                }
            }
            foreach (var expected in new[] { 16, 24, 32, 48, 64, 128, 256 })
                Assert(sizes.Contains(expected), "application ICO must include a " + expected + "px frame");

            var sourcePath = FindRepositoryFile(Path.Combine("src", "LegacySift", "Assets", "legacysift-icon-source.png"));
            var documentedSourcePath = FindRepositoryFile(Path.Combine("docs", "assets", "legacysift-icon-source.png"));
            using (var source = Image.FromFile(sourcePath))
            {
                Assert(source.Width == 1254 && source.Height == 1254, "official icon source must preserve its supplied 1254px canvas");
                Assert(source.RawFormat.Guid == ImageFormat.Png.Guid, "official icon source must remain PNG");
            }
            Assert(BytesEqual(File.ReadAllBytes(sourcePath), File.ReadAllBytes(documentedSourcePath)), "project and documentation icon sources must be byte-identical");
            foreach (var expected in new[] { 16, 24, 32, 48, 64, 128, 256 })
            {
                var pngPath = FindRepositoryFile(Path.Combine("docs", "assets", "icon-sizes", "legacysift-icon-" + expected + ".png"));
                using (var image = Image.FromFile(pngPath))
                    Assert(image.Width == expected && image.Height == expected, "documentation icon must preserve the " + expected + "px size");
            }

            Assert(File.Exists(FindRepositoryFile(Path.Combine("docs", "assets", "legacysift-icon-qa.png"))), "actual-size icon QA contact sheet must be checked in");
            Assert(File.Exists(FindRepositoryFile(Path.Combine("docs", "assets", "legacysift-logo-light.png"))), "cropped full logo asset must be checked in");
            Assert(!File.Exists(Path.Combine(Path.GetDirectoryName(sourcePath), "legacysift-icon.svg")), "retired application icon SVG must be absent");
            Assert(!File.Exists(Path.Combine(Path.GetDirectoryName(documentedSourcePath), "legacysift-mark.svg")), "retired alternate icon mark must be absent");
        }

        private static void TestScriptCoverage()
        {
            Assert(Enum.GetNames(typeof(AppLanguage)).All(x => !x.Equals("Russian", StringComparison.OrdinalIgnoreCase)), "Russian enum value must be absent");
            Assert(LanguageCatalog.All.All(x => !x.Code.Equals("RU", StringComparison.OrdinalIgnoreCase)), "Russian catalog entry must be absent");
            Assert(L10n.T("LanguageButton") == "Language / Lingua…", "language recovery entry must remain bilingual");
            Assert(L10n.T("ThemeButton") == "Theme / Tema…", "theme recovery entry must remain bilingual");

            var scripts = new Dictionary<AppLanguage, string>
            {
                { AppLanguage.Turkish, "İ" }, { AppLanguage.Ukrainian, "Ї" }, { AppLanguage.ChineseSimplified, "旧" },
                { AppLanguage.Japanese, "古" }, { AppLanguage.Hindi, "पुराना" }, { AppLanguage.Greek, "ΠΑΛΙ" },
                { AppLanguage.Korean, "이전" }, { AppLanguage.Vietnamese, "HIỆN TẠI" },
                { AppLanguage.Danish, "æ" }, { AppLanguage.NorwegianBokmal, "å" }, { AppLanguage.Finnish, "Ä" },
                { AppLanguage.Slovak, "ľ" }, { AppLanguage.Bulgarian, "СТАР" }, { AppLanguage.Croatian, "Č" },
                { AppLanguage.Bengali, "পুরনো" }, { AppLanguage.Thai, "เก่า" }, { AppLanguage.Malay, "SEMASA" },
                { AppLanguage.Filipino, "KASALUKUYANG" }, { AppLanguage.Estonian, "Ä" },
                { AppLanguage.Latvian, "Ā" }, { AppLanguage.Lithuanian, "ų" }
            };
            foreach (var pair in scripts)
            {
                var dict = L10n.TranslationForTests(pair.Key);
                Assert(dict.Values.Any(x => x.Contains(pair.Value)), pair.Key + " must contain its expected script or diacritics");
                Assert(dict.Values.All(x => !x.Contains("\uFFFD")), pair.Key + " must not contain replacement glyphs");
                Assert(dict.Values.All(x => !x.Contains("\u25A1")), pair.Key + " must not contain a literal missing-glyph square");
                using (var font = new Font("Segoe UI", 9F))
                    Assert(TextRenderer.MeasureText(pair.Value, font).Width > 4, pair.Key + " script sample must be measurable by WinForms");
            }

            foreach (var info in LanguageCatalog.All)
            {
                var terms = SafetyTerms(info.Language);
                var dict = L10n.TranslationForTests(info.Language);
                var critical = dict["Intro"] + " " + dict["CleanupExplanation"] + " " + dict["Confirm"] + " " + dict["ExactNote"];
                Assert(!string.IsNullOrWhiteSpace(dict["ThemeTitle"]), info.Code + " theme title must be localized");
                Assert(!string.IsNullOrWhiteSpace(dict["ThemeSystem"]), info.Code + " system theme choice must be localized");
                Assert(!string.IsNullOrWhiteSpace(dict["ThemeLight"]), info.Code + " light theme choice must be localized");
                Assert(!string.IsNullOrWhiteSpace(dict["ThemeDark"]), info.Code + " dark theme choice must be localized");
                Assert(ContainsIgnoreCase(critical, terms[0]), info.Code + " critical wording must identify OLD");
                Assert(ContainsIgnoreCase(critical, terms[1]), info.Code + " critical wording must identify CURRENT");
                Assert(ContainsIgnoreCase(critical, terms[2]), info.Code + " critical wording must identify identical copies");
            }
        }

        private static string[] SafetyTerms(AppLanguage language)
        {
            switch (language)
            {
                case AppLanguage.Italian: return new[] { "VECCHIA", "ATTUALE", "identic" };
                case AppLanguage.German: return new[] { "ALTEN", "AKTUELL", "identisch" };
                case AppLanguage.French: return new[] { "ANCIEN", "ACTUEL", "identique" };
                case AppLanguage.Spanish: return new[] { "ANTIGUA", "ACTUAL", "idéntic" };
                case AppLanguage.Portuguese: return new[] { "ANTIGA", "ATUAL", "idêntic" };
                case AppLanguage.Polish: return new[] { "STARY", "BIEŻĄC", "identycz" };
                case AppLanguage.Dutch: return new[] { "OUDE", "HUIDIGE", "identiek" };
                case AppLanguage.Turkish: return new[] { "ESKİ", "GÜNCEL", "aynı" };
                case AppLanguage.Ukrainian: return new[] { "СТАР", "ПОТОЧН", "ідентич" };
                case AppLanguage.ChineseSimplified: return new[] { "旧", "当前", "相同" };
                case AppLanguage.Japanese: return new[] { "古い", "現在", "同一" };
                case AppLanguage.Hindi: return new[] { "पुरान", "वर्तमान", "समान" };
                case AppLanguage.Romanian: return new[] { "VECHI", "CURENT", "identic" };
                case AppLanguage.Czech: return new[] { "STAR", "AKTUÁLN", "totož" };
                case AppLanguage.Greek: return new[] { "ΠΑΛΙ", "ΤΡΕΧ", "πανομοιότυπ" };
                case AppLanguage.Hungarian: return new[] { "RÉGI", "JELENLEGI", "azonos" };
                case AppLanguage.Swedish: return new[] { "GAMMAL", "AKTUELL", "identisk" };
                case AppLanguage.Korean: return new[] { "이전", "현재", "동일" };
                case AppLanguage.Indonesian: return new[] { "LAMA", "SAAT INI", "identik" };
                case AppLanguage.Vietnamese: return new[] { "CŨ", "HIỆN TẠI", "giống hệt" };
                case AppLanguage.Danish: return new[] { "GAMLE", "NUVÆRENDE", "identisk" };
                case AppLanguage.NorwegianBokmal: return new[] { "GAMLE", "GJELDENDE", "identisk" };
                case AppLanguage.Finnish: return new[] { "VANH", "NYKYI", "identt" };
                case AppLanguage.Slovak: return new[] { "STAR", "AKTUÁLN", "identick" };
                case AppLanguage.Bulgarian: return new[] { "СТАР", "ТЕКУЩ", "идентич" };
                case AppLanguage.Croatian: return new[] { "STAR", "TRENUTAČ", "identič" };
                case AppLanguage.Bengali: return new[] { "পুরনো", "বর্তমান", "হুবহু" };
                case AppLanguage.Thai: return new[] { "เก่า", "ปัจจุบัน", "เหมือนกัน" };
                case AppLanguage.Malay: return new[] { "LAMA", "SEMASA", "sama" };
                case AppLanguage.Filipino: return new[] { "LUMANG", "KASALUKUYANG", "magkapareho" };
                case AppLanguage.Estonian: return new[] { "VANA", "PRAEGU", "ident" };
                case AppLanguage.Latvian: return new[] { "VEC", "PAŠREIZ", "identisk" };
                case AppLanguage.Lithuanian: return new[] { "SEN", "DABART", "identišk" };
                default: return new[] { "OLD", "CURRENT", "identical" };
            }
        }

        private static bool ContainsIgnoreCase(string text, string value)
        {
            return text.IndexOf(value, StringComparison.CurrentCultureIgnoreCase) >= 0;
        }

        private static void TestLayoutMatrix()
        {
            var startupConfigurations = new[]
            {
                new LayoutConfiguration("1366x768@100", new Size(1144, 673), 1F),
                new LayoutConfiguration("1600x900@100", new Size(1144, 701), 1F),
                new LayoutConfiguration("1920x1080@100", new Size(1144, 701), 1F),
                new LayoutConfiguration("2560x1440@100", new Size(1144, 701), 1F),
                new LayoutConfiguration("1600x900@125-approx", new Size(1434, 825), 1.25F),
                new LayoutConfiguration("1920x1080@150-approx", new Size(1724, 1001), 1.5F),
                new LayoutConfiguration("2560x1440@150-approx", new Size(1724, 1051), 1.5F)
            };
            var allStates = (LayoutTestState[])Enum.GetValues(typeof(LayoutTestState));

            foreach (var theme in new[] { ThemeMode.Light, ThemeMode.Dark })
            {
                ThemeManager.SetMode(theme);
                foreach (var info in LanguageCatalog.All)
                {
                    foreach (var configuration in startupConfigurations)
                        AuditMainForm(theme, info, configuration, LayoutTestState.Initial);

                    var constrained = startupConfigurations[0];
                    foreach (var state in allStates.Where(x => x != LayoutTestState.Initial))
                        AuditMainForm(theme, info, constrained, state);

                    AuditMainForm(theme, info, startupConfigurations[4], LayoutTestState.AnalysisCompleted);
                    AuditMainForm(theme, info, startupConfigurations[4], LayoutTestState.OtherOptionsExpanded);
                    AuditMainForm(theme, info, startupConfigurations[5], LayoutTestState.AnalysisCompleted);
                    AuditMainForm(theme, info, startupConfigurations[5], LayoutTestState.OtherOptionsExpanded);
                }
            }
            ThemeManager.SetMode(ThemeMode.System);
            L10n.SetLanguage(AppLanguage.English);
        }

        private static void AuditMainForm(ThemeMode theme, LanguageInfo info, LayoutConfiguration configuration, LayoutTestState state)
        {
            L10n.SetLanguage(info.Language);
            using (var form = new MainForm())
            {
                form.PrepareLayoutTest(state, configuration.ClientSize, configuration.ScaleFactor);
                if (Math.Abs(configuration.ScaleFactor - 1F) > 0.001F)
                {
                    // The deterministic harness disables WinForms AutoScale.
                    // Reproduce the DPI-scaled height of this fixed summary
                    // panel after the form has materialized on the runner.
                    var summaryPanel = Find(form, "SummaryPanel");
                    summaryPanel.Height = (int)Math.Ceiling(summaryPanel.Height * configuration.ScaleFactor);
                    summaryPanel.Parent.PerformLayout();
                    form.PerformLayout();
                    Application.DoEvents();
                }
                var prefix = theme + " " + info.Code + " " + configuration.Name + " " + state + ": ";
                var required = new[]
                {
                    "HeaderMark", "ThemeButton", "LanguageButton", "MainTabs", "WorkPage", "OldPanel", "CurrentPanel", "OldBrowseButton",
                    "CurrentBrowseButton", "AnalyzeButton", "CancelButton", "ReportButton", "ProgressBar", "ResultsTabs", "CleanupGroup", "CleanupExplanation", "SafetyFolderRadio",
                    "ConfirmCheck", "CleanupButton", "RestoreButton", "ProtectedReminder", "SummaryLabel", "HelpText"
                };
                foreach (var name in required)
                {
                    var control = Find(form, name);
                    Assert(control != null, prefix + name + " must exist");
                    Assert(control.Width > 2 && control.Height > 2, prefix + name + " must have visible area");
                    Assert(IsInsideForm(form, control, 5), prefix + name + " must stay inside the client area; bounds=" + BoundsInForm(control, form) + ", client=" + form.ClientSize);
                }

                Assert(!BoundsInForm(Find(form, "OldPanel"), form).IntersectsWith(BoundsInForm(Find(form, "CurrentPanel"), form)), prefix + "OLD and CURRENT panels must not overlap");
                Assert(!BoundsInForm(Find(form, "OldBrowseButton"), form).IntersectsWith(BoundsInForm(Find(form, "CurrentBrowseButton"), form)), prefix + "Browse buttons must not overlap");
                var resultBounds = BoundsInForm(Find(form, "ResultsTabs"), form);
                var cleanupBounds = BoundsInForm(Find(form, "CleanupGroup"), form);
                Assert(!resultBounds.IntersectsWith(cleanupBounds), prefix + "results and cleanup must not overlap; results=" + resultBounds + ", cleanup=" + cleanupBounds);
                Assert(ButtonTextFits((Button)Find(form, "ThemeButton"), 12), prefix + "theme button text must fit");
                Assert(ButtonTextFits((Button)Find(form, "LanguageButton"), 12), prefix + "language button text must fit");
                Assert(ButtonTextFits((Button)Find(form, "OldBrowseButton"), 12), prefix + "OLD Browse text must fit");
                Assert(ButtonTextFits((Button)Find(form, "CurrentBrowseButton"), 12), prefix + "CURRENT Browse text must fit");
                Assert(ButtonTextFits((Button)Find(form, "AnalyzeButton"), 16), prefix + "check button text must fit");
                Assert(ButtonTextFits((Button)Find(form, "CleanupButton"), 16), prefix + "cleanup button text must fit");
                Assert(LabelTextFits((Label)Find(form, "SummaryLabel")), prefix + "summary text must fit");
                Assert(LabelTextFits((Label)Find(form, "CleanupExplanation")), prefix + "cleanup explanation must fit");
                Assert(LabelTextFits((Label)Find(form, "ProtectedReminder")), prefix + "protected reminder must fit");
                Assert(Find(form, "HelpText").Text.Length > 300, prefix + "guide and safety text must be present");
                Assert(form.Icon != null, prefix + "window and taskbar icon must be assigned");

                var palette = ThemeManager.CurrentPalette;
                Assert(Find(form, "WorkPage").BackColor == palette.AppBackground, prefix + "work page must use the active application surface");
                Assert(Find(form, "OldPanel").BackColor == palette.OldSurface, prefix + "OLD panel must use the active semantic surface");
                Assert(Find(form, "CurrentPanel").BackColor == palette.CurrentSurface, prefix + "CURRENT panel must use the active semantic surface");
                Assert(Find(form, "MainTabs") is ThemedTabControl, prefix + "main navigation must use the focus-aware themed tab control");
                Assert(Find(form, "ResultsTabs") is ThemedTabControl, prefix + "result navigation must use the focus-aware themed tab control");
                foreach (var buttonName in new[] { "ThemeButton", "LanguageButton", "OldBrowseButton", "CurrentBrowseButton", "AnalyzeButton", "CancelButton", "ReportButton", "CleanupButton", "RestoreButton" })
                    Assert(Find(form, buttonName) is ThemedButton, prefix + buttonName + " must use theme-aware disabled rendering");
                Assert(Find(form, "ConfirmCheck") is ThemedCheckBox, prefix + "confirmation must use theme-aware disabled rendering");
                Assert(Find(form, "ProgressBar") is ThemedProgressBar, prefix + "progress must use the active palette");
                var mainTabs = (TabControl)Find(form, "MainTabs");
                var resultsTabs = (TabControl)Find(form, "ResultsTabs");
                Assert(TabTextFits(mainTabs), prefix + "main tab labels must remain fully visible; " + DescribeTabMetrics(mainTabs));
                Assert(TabTextFits(resultsTabs), prefix + "result tab labels must remain fully visible; " + DescribeTabMetrics(resultsTabs));

                foreach (var buttonName in new[] { "ThemeButton", "LanguageButton", "OldBrowseButton", "CurrentBrowseButton", "AnalyzeButton", "CleanupButton", "RestoreButton" })
                {
                    var button = (Button)Find(form, buttonName);
                    Assert(button.FlatStyle == FlatStyle.Flat && button.FlatAppearance.BorderSize == 1, prefix + buttonName + " must preserve a visible non-color boundary");
                    Assert(button.TabStop, prefix + buttonName + " must remain keyboard-focusable");
                }

                if (state == LayoutTestState.AnalysisCompleted && configuration.Name == "1366x768@100")
                {
                    var grid = (DataGridView)Find(form, "UniqueGrid");
                    var usefulMinimum = grid.ColumnHeadersHeight + grid.RowTemplate.Height * 4;
                    if (info.Language == AppLanguage.English)
                    {
                        var allocationNames = new[] { "IntroLabel", "FolderPair", "CheckArea", "SummaryPanel", "ResultsTabs", "CleanupGroup" };
                        Console.WriteLine("LAYOUT_ALLOCATION theme=" + theme + " " + string.Join(" ", allocationNames.Select(name => name + "=" + BoundsInForm(Find(form, name), form))));
                    }
                    Assert(grid.Rows.Count >= 4, prefix + "test data must expose at least four actual result rows");
                    var rowTextHeight = TextRenderer.MeasureText("Ag", grid.Font).Height;
                    Assert(grid.RowTemplate.Height >= rowTextHeight + 2, prefix + "compact result rows must retain vertical text breathing room");
                    if (grid.ClientSize.Height < usefulMinimum)
                    {
                        Console.WriteLine("LAYOUT_SHORT " + prefix +
                                          " results=" + resultBounds +
                                          " explanation=" + BoundsInForm(Find(form, "ResultExplanation"), form) +
                                          " tabs=" + DescribeTabMetrics((TabControl)Find(form, "ResultsTabs")));
                    }
                    Assert(grid.ClientSize.Height >= usefulMinimum, prefix + "result grid must show its header and at least four data rows; height=" + grid.ClientSize.Height + ", minimum=" + usefulMinimum);
                    Assert(grid.DisplayedRowCount(false) >= 4, prefix + "at least four result rows must be visibly displayed");
                    if (info.Language == AppLanguage.English)
                        Console.WriteLine("LAYOUT_METRIC theme=" + theme + " constrained-grid-height=" + grid.ClientSize.Height + " displayed-rows=" + grid.DisplayedRowCount(false) + " cleanup-height=" + Find(form, "CleanupGroup").Height);
                }

                if (state == LayoutTestState.OtherOptionsExpanded)
                {
                    var options = Find(form, "OtherOptionsPanel");
                    Assert(options != null && options.Width > 2 && options.Height > 2, prefix + "expanded options must have visible area");
                    Assert(IsInsideForm(form, options, 5), prefix + "expanded options must stay inside the client area");
                }
            }
        }

        private static void TestLanguageDialogLayout()
        {
            foreach (var theme in new[] { ThemeMode.Light, ThemeMode.Dark })
            {
                ThemeManager.SetMode(theme);
                foreach (var current in LanguageCatalog.All)
                {
                    L10n.SetLanguage(current.Language);
                    using (var dialog = new LanguageDialog(current.Language))
                    {
                        var prefix = theme + " " + current.Code + ": ";
                        dialog.CreateControl();
                        dialog.Show();
                        Application.DoEvents();
                        ThemeManager.ApplyTo(dialog);
                        dialog.PerformLayout();
                        var choices = dialog.Controls.Find("LanguageList", true)[0].Controls.OfType<RadioButton>().ToList();
                        Assert(choices.Count == 34, prefix + "language dialog must contain 34 choices");
                        foreach (var choice in choices)
                        {
                            Assert(choice.Width > 250 && choice.Height >= 30, prefix + "language choice must have usable bounds: " + choice.Name);
                            var measured = TextRenderer.MeasureText(choice.Text, choice.Font).Width + 55;
                            Assert(measured <= choice.ClientSize.Width + 8, prefix + "language choice text must fit: " + choice.Name);
                        }
                        Assert(choices.Any(x => x.Name == "LanguageChoice_IT"), prefix + "dialog must always expose Italian");
                        Assert(choices.Any(x => x.Name == "LanguageChoice_EN"), prefix + "dialog must always expose English");
                        Assert(choices[0].Name == "LanguageChoice_EN" && choices[1].Name == "LanguageChoice_IT", prefix + "dialog must keep recovery languages first");
                        Assert(dialog.Icon != null, prefix + "language dialog must use the application icon");
                        Assert(dialog.BackColor == ThemeManager.CurrentPalette.AppBackground, prefix + "language dialog must use the active theme");
                        var scroll = (ScrollableControl)dialog.Controls.Find("LanguageScroll", true)[0];
                        var last = choices.Last();
                        scroll.ScrollControlIntoView(last);
                        Application.DoEvents();
                        Assert(last.Bottom + scroll.AutoScrollPosition.Y <= scroll.ClientSize.Height + 8, prefix + "final language entry must be reachable by scrolling");
                    }
                }
            }
            ThemeManager.SetMode(ThemeMode.System);
            L10n.SetLanguage(AppLanguage.English);
        }

        private static void TestThemeDialogLayout()
        {
            foreach (var theme in new[] { ThemeMode.Light, ThemeMode.Dark })
            {
                ThemeManager.SetMode(theme);
                foreach (var current in LanguageCatalog.All)
                {
                    L10n.SetLanguage(current.Language);
                    using (var dialog = new ThemeDialog(theme))
                    {
                        var prefix = theme + " " + current.Code + ": ";
                        dialog.Show();
                        Application.DoEvents();
                        ThemeManager.ApplyTo(dialog);
                        dialog.PerformLayout();
                        var choices = new[] { "ThemeSystem", "ThemeLight", "ThemeDark" }.Select(name => (RadioButton)Find(dialog, name)).ToArray();
                        Assert(choices.All(x => x != null), prefix + "theme dialog must contain all three choices");
                        Assert(choices.All(x => !string.IsNullOrWhiteSpace(x.Text)), prefix + "theme choices must be localized");
                        Assert(choices.All(x => TextRenderer.MeasureText(x.Text, x.Font).Width + 45 <= x.ClientSize.Width + 8), prefix + "theme choice text must fit");
                        Assert(choices.Count(x => x.Checked) == 1, prefix + "exactly one theme choice must be selected");
                        Assert(dialog.SelectedMode == theme, prefix + "current explicit theme must be selected");
                        Assert(dialog.Icon != null, prefix + "theme dialog must use the application icon");
                        Assert(dialog.BackColor == ThemeManager.CurrentPalette.AppBackground, prefix + "theme dialog must use the active theme");
                        foreach (var name in new[] { "ThemeHeading", "ThemeSystem", "ThemeLight", "ThemeDark", "ThemeOk", "ThemeCancel" })
                            Assert(IsInsideForm(dialog, Find(dialog, name), 2), prefix + name + " must stay inside the dialog");
                    }
                }
            }
            ThemeManager.SetMode(ThemeMode.System);
            L10n.SetLanguage(AppLanguage.English);
        }

        private static void CaptureRepresentativeLayouts(string outputDirectory)
        {
            Directory.CreateDirectory(outputDirectory);
            CaptureThemeLayouts(outputDirectory, ThemeMode.Light, new[]
            {
                AppLanguage.Italian, AppLanguage.German, AppLanguage.Ukrainian,
                AppLanguage.ChineseSimplified, AppLanguage.Hindi, AppLanguage.Korean,
                AppLanguage.Bengali, AppLanguage.Thai, AppLanguage.Lithuanian
            });
            CaptureThemeLayouts(outputDirectory, ThemeMode.Dark, new[]
            {
                AppLanguage.Italian, AppLanguage.German, AppLanguage.ChineseSimplified,
                AppLanguage.Hindi, AppLanguage.Korean
            });

            foreach (var theme in new[] { ThemeMode.Light, ThemeMode.Dark })
            {
                CaptureLayout(outputDirectory, theme, AppLanguage.Italian, LayoutTestState.Initial);
                CaptureLayout(outputDirectory, theme, AppLanguage.Italian, LayoutTestState.AnalysisRunning);
            }

            ThemeManager.SetMode(ThemeMode.System);
            L10n.SetLanguage(AppLanguage.English);
        }

        private static void CaptureThemeLayouts(string outputDirectory, ThemeMode theme, IEnumerable<AppLanguage> languages)
        {
            foreach (var language in languages)
                CaptureLayout(outputDirectory, theme, language, LayoutTestState.AnalysisCompleted);
        }

        private static void CaptureLayout(string outputDirectory, ThemeMode theme, AppLanguage language, LayoutTestState state)
        {
            ThemeManager.SetMode(theme);
            L10n.SetLanguage(language);
            using (var form = new MainForm())
            {
                form.PrepareLayoutTest(state, new Size(1144, 673), 1F);
                using (var image = form.CaptureLayoutTestImage())
                {
                    var fileName = theme.ToString().ToLowerInvariant() + "-" + L10n.ToCode(language).Replace("-", "_") + "-" +
                                   state.ToString().ToLowerInvariant() + "-1366x768-100.png";
                    image.Save(Path.Combine(outputDirectory, fileName), ImageFormat.Png);
                }
            }
        }

        private static Control Find(Control root, string name)
        {
            return root.Controls.Find(name, true).FirstOrDefault();
        }

        private static string FindRepositoryFile(string relativePath)
        {
            foreach (var start in new[] { Directory.GetCurrentDirectory(), AppDomain.CurrentDomain.BaseDirectory })
            {
                var directory = new DirectoryInfo(start);
                for (var depth = 0; directory != null && depth < 10; depth++, directory = directory.Parent)
                {
                    var candidate = Path.Combine(directory.FullName, relativePath);
                    if (File.Exists(candidate)) return candidate;
                }
            }
            throw new FileNotFoundException("Could not locate repository file", relativePath);
        }

        private static bool IsInsideForm(Form form, Control control, int tolerance)
        {
            if (control == null) return false;
            var bounds = BoundsInForm(control, form);
            return bounds.Left >= -tolerance && bounds.Top >= -tolerance &&
                   bounds.Right <= form.ClientSize.Width + tolerance && bounds.Bottom <= form.ClientSize.Height + tolerance;
        }

        private static Rectangle BoundsInForm(Control control, Form form)
        {
            var point = Point.Empty;
            var current = control;
            while (current != null && current != form)
            {
                point.Offset(current.Left, current.Top);
                current = current.Parent;
            }
            return new Rectangle(point, control.Size);
        }

        private static bool ButtonTextFits(Button button, int horizontalPadding)
        {
            var measured = TextRenderer.MeasureText(button.Text ?? string.Empty, button.Font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.SingleLine).Width;
            return measured <= button.ClientSize.Width - horizontalPadding + 8;
        }

        private static bool LabelTextFits(Label label)
        {
            var proposed = new Size(System.Math.Max(1, label.ClientSize.Width), int.MaxValue);
            var measured = TextRenderer.MeasureText(label.Text ?? string.Empty, label.Font, proposed, TextFormatFlags.WordBreak);
            return measured.Height <= label.ClientSize.Height + 8;
        }

        private static bool TabTextFits(TabControl tabs)
        {
            for (var index = 0; index < tabs.TabPages.Count; index++)
            {
                var bounds = tabs.GetTabRect(index);
                var measured = MeasureTabText(tabs.TabPages[index].Text, tabs.Font);
                if (bounds.Right > tabs.ClientSize.Width + 2 || measured > bounds.Width) return false;
            }
            return true;
        }

        private static string DescribeTabMetrics(TabControl tabs)
        {
            return string.Join(", ", tabs.TabPages.Cast<TabPage>().Select((page, index) =>
            {
                var bounds = tabs.GetTabRect(index);
                var measured = MeasureTabText(page.Text, tabs.Font);
                return page.Text + " measured=" + measured + " bounds=" + bounds;
            }));
        }

        private static int MeasureTabText(string text, Font font)
        {
            return TextRenderer.MeasureText(text ?? string.Empty, font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.SingleLine | TextFormatFlags.NoPadding).Width;
        }

        private sealed class LayoutConfiguration
        {
            public string Name { get; private set; }
            public Size ClientSize { get; private set; }
            public float ScaleFactor { get; private set; }

            public LayoutConfiguration(string name, Size clientSize, float scaleFactor)
            {
                Name = name;
                ClientSize = clientSize;
                ScaleFactor = scaleFactor;
            }
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
