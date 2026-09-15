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
                TestScriptCoverage();
                TestLayoutMatrix();
                TestLanguageDialogLayout();
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
            Assert(LanguageCatalog.All.Count == 34, "the final 0.2.2-alpha catalog must contain exactly 34 languages");
            Assert(english.Count == 116, "English baseline must contain the frozen 116-key UI contract");
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

        private static void TestScriptCoverage()
        {
            Assert(Enum.GetNames(typeof(AppLanguage)).All(x => !x.Equals("Russian", StringComparison.OrdinalIgnoreCase)), "Russian enum value must be absent");
            Assert(LanguageCatalog.All.All(x => !x.Code.Equals("RU", StringComparison.OrdinalIgnoreCase)), "Russian catalog entry must be absent");
            Assert(L10n.T("LanguageButton") == "Language / Lingua…", "language recovery entry must remain bilingual");

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

            foreach (var info in LanguageCatalog.All)
            {
                foreach (var configuration in startupConfigurations)
                    AuditMainForm(info, configuration, LayoutTestState.Initial);

                var constrained = startupConfigurations[0];
                foreach (var state in allStates.Where(x => x != LayoutTestState.Initial))
                    AuditMainForm(info, constrained, state);

                AuditMainForm(info, startupConfigurations[4], LayoutTestState.AnalysisCompleted);
                AuditMainForm(info, startupConfigurations[4], LayoutTestState.OtherOptionsExpanded);
                AuditMainForm(info, startupConfigurations[5], LayoutTestState.AnalysisCompleted);
                AuditMainForm(info, startupConfigurations[5], LayoutTestState.OtherOptionsExpanded);
            }
            L10n.SetLanguage(AppLanguage.English);
        }

        private static void AuditMainForm(LanguageInfo info, LayoutConfiguration configuration, LayoutTestState state)
        {
            L10n.SetLanguage(info.Language);
            using (var form = new MainForm())
            {
                form.PrepareLayoutTest(state, configuration.ClientSize, configuration.ScaleFactor);
                var prefix = info.Code + " " + configuration.Name + " " + state + ": ";
                var required = new[]
                {
                    "LanguageButton", "MainTabs", "WorkPage", "OldPanel", "CurrentPanel", "OldBrowseButton",
                    "CurrentBrowseButton", "AnalyzeButton", "ResultsTabs", "CleanupGroup", "CleanupExplanation", "SafetyFolderRadio",
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
                Assert(ButtonTextFits((Button)Find(form, "LanguageButton"), 12), prefix + "language button text must fit");
                Assert(ButtonTextFits((Button)Find(form, "OldBrowseButton"), 12), prefix + "OLD Browse text must fit");
                Assert(ButtonTextFits((Button)Find(form, "CurrentBrowseButton"), 12), prefix + "CURRENT Browse text must fit");
                Assert(ButtonTextFits((Button)Find(form, "AnalyzeButton"), 16), prefix + "check button text must fit");
                Assert(ButtonTextFits((Button)Find(form, "CleanupButton"), 16), prefix + "cleanup button text must fit");
                Assert(LabelTextFits((Label)Find(form, "SummaryLabel")), prefix + "summary text must fit");
                Assert(LabelTextFits((Label)Find(form, "CleanupExplanation")), prefix + "cleanup explanation must fit");
                Assert(LabelTextFits((Label)Find(form, "ProtectedReminder")), prefix + "protected reminder must fit");
                Assert(Find(form, "HelpText").Text.Length > 300, prefix + "guide and safety text must be present");

                if (state == LayoutTestState.AnalysisCompleted && configuration.Name == "1366x768@100")
                {
                    var grid = (DataGridView)Find(form, "UniqueGrid");
                    var usefulMinimum = grid.ColumnHeadersHeight + grid.RowTemplate.Height * 3;
                    Assert(grid.Rows.Count >= 4, prefix + "test data must expose at least four actual result rows");
                    Assert(grid.ClientSize.Height >= usefulMinimum, prefix + "result grid must show its header and at least three data rows; height=" + grid.ClientSize.Height + ", minimum=" + usefulMinimum);
                    Assert(grid.DisplayedRowCount(false) >= 3, prefix + "at least three result rows must be visibly displayed");
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
            foreach (var current in LanguageCatalog.All)
            {
                using (var dialog = new LanguageDialog(current.Language))
                {
                    dialog.CreateControl();
                    dialog.Show();
                    Application.DoEvents();
                    dialog.PerformLayout();
                    var choices = dialog.Controls.Find("LanguageList", true)[0].Controls.OfType<RadioButton>().ToList();
                    Assert(choices.Count == 34, current.Code + " language dialog must contain 34 choices");
                    foreach (var choice in choices)
                    {
                        Assert(choice.Width > 250 && choice.Height >= 30, current.Code + " language choice must have usable bounds: " + choice.Name);
                        var measured = TextRenderer.MeasureText(choice.Text, choice.Font).Width + 55;
                        Assert(measured <= choice.ClientSize.Width + 8, current.Code + " language choice text must fit: " + choice.Name);
                    }
                    Assert(choices.Any(x => x.Name == "LanguageChoice_IT"), current.Code + " dialog must always expose Italian");
                    Assert(choices.Any(x => x.Name == "LanguageChoice_EN"), current.Code + " dialog must always expose English");
                    Assert(choices[0].Name == "LanguageChoice_EN" && choices[1].Name == "LanguageChoice_IT", current.Code + " dialog must keep recovery languages first");
                    var scroll = (ScrollableControl)dialog.Controls.Find("LanguageScroll", true)[0];
                    var last = choices.Last();
                    scroll.ScrollControlIntoView(last);
                    Application.DoEvents();
                    Assert(last.Bottom + scroll.AutoScrollPosition.Y <= scroll.ClientSize.Height + 8, current.Code + " final language entry must be reachable by scrolling");
                }
            }
        }

        private static void CaptureRepresentativeLayouts(string outputDirectory)
        {
            Directory.CreateDirectory(outputDirectory);
            var languages = new[]
            {
                AppLanguage.Italian, AppLanguage.German, AppLanguage.Ukrainian,
                AppLanguage.ChineseSimplified, AppLanguage.Hindi, AppLanguage.Korean,
                AppLanguage.Bengali, AppLanguage.Thai, AppLanguage.Lithuanian
            };
            foreach (var language in languages)
            {
                L10n.SetLanguage(language);
                using (var form = new MainForm())
                {
                    form.PrepareLayoutTest(LayoutTestState.AnalysisCompleted, new Size(1144, 673), 1F);
                    using (var image = form.CaptureLayoutTestImage())
                        image.Save(Path.Combine(outputDirectory, L10n.ToCode(language).Replace("-", "_") + "-1366x768-100.png"), ImageFormat.Png);
                }
            }
            L10n.SetLanguage(AppLanguage.English);
        }

        private static Control Find(Control root, string name)
        {
            return root.Controls.Find(name, true).FirstOrDefault();
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
