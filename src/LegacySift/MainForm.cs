using LegacySift.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LegacySift
{
    internal sealed class MainForm : Form
    {
        private TextBox _sourceBox;
        private TextBox _referenceBox;
        private Button _analyzeButton;
        private Button _sourceBrowseButton;
        private Button _referenceBrowseButton;
        private Button _cleanupButton;
        private Button _cancelButton;
        private Button _reportButton;
        private Button _restoreButton;
        private Button _languageButton;
        private RadioButton _quarantineRadio;
        private RadioButton _recycleRadio;
        private CheckBox _removeEmptyCheck;
        private CheckBox _confirmCheck;
        private LinkLabel _otherOptionsLink;
        private Panel _otherOptionsPanel;
        private Label _summaryLabel;
        private Label _statusLabel;
        private ProgressBar _progress;
        private TabControl _resultsTabs;
        private DataGridView _uniqueGrid;
        private DataGridView _versionGrid;
        private DataGridView _duplicateGrid;
        private DataGridView _errorGrid;

        private CancellationTokenSource _cts;
        private AnalysisResult _analysis;
        private string _lastReportPath;
        private string _lastQuarantineRoot;
        private bool _busy;

        public MainForm()
        {
            Text = L10n.T("AppTitle");
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(980, 720);
            Size = new Size(1200, 860);
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            BuildUi();
        }

        private void BuildUi()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(0)
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            Controls.Add(root);

            var header = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 2,
                Padding = new Padding(14, 10, 14, 8),
                BackColor = SystemColors.ControlLightLight
            };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            var title = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 17F, FontStyle.Bold),
                Text = "LegacySift"
            };
            _languageButton = new Button
            {
                Text = L10n.T("LanguageButton"),
                AutoSize = true,
                Margin = new Padding(10, 5, 0, 0)
            };
            _languageButton.Click += ChangeLanguage;
            header.Controls.Add(title, 0, 0);
            header.Controls.Add(_languageButton, 1, 0);
            root.Controls.Add(header, 0, 0);

            var mainTabs = new TabControl { Dock = DockStyle.Fill };
            var workPage = new TabPage(L10n.T("TabWork"));
            var helpPage = new TabPage(L10n.T("TabHelp"));
            mainTabs.TabPages.Add(workPage);
            mainTabs.TabPages.Add(helpPage);
            root.Controls.Add(mainTabs, 0, 1);

            var outer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 10,
                Padding = new Padding(14),
                AutoScroll = true
            };
            outer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            outer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            outer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            outer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            outer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            outer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            outer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            outer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            outer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            workPage.Controls.Add(outer);

            var intro = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(1120, 0),
                Font = new Font(Font.FontFamily, 10F, FontStyle.Bold),
                Text = L10n.T("Intro"),
                Padding = new Padding(2, 2, 2, 8)
            };
            outer.Controls.Add(intro, 0, 0);

            outer.Controls.Add(CreateFolderPanel(
                L10n.T("OldTitle"),
                L10n.T("OldBadge"),
                L10n.T("OldDescription"),
                Color.FromArgb(255, 248, 225),
                Color.FromArgb(129, 77, 0),
                out _sourceBox,
                out _sourceBrowseButton,
                BrowseSource), 0, 1);

            outer.Controls.Add(CreateFolderPanel(
                L10n.T("CurrentTitle"),
                L10n.T("CurrentBadge"),
                L10n.T("CurrentDescription"),
                Color.FromArgb(232, 245, 233),
                Color.FromArgb(27, 94, 32),
                out _referenceBox,
                out _referenceBrowseButton,
                BrowseReference), 0, 2);

            _sourceBox.TextChanged += PathsChanged;
            _referenceBox.TextChanged += PathsChanged;

            var direction = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(1120, 0),
                Text = "↓  " + L10n.T("Direction"),
                Font = new Font(Font, FontStyle.Bold),
                Padding = new Padding(4, 8, 4, 4)
            };
            outer.Controls.Add(direction, 0, 3);

            var actionBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Padding = new Padding(0, 5, 0, 5)
            };
            _analyzeButton = new Button
            {
                Text = L10n.T("Analyze"),
                AutoSize = true,
                MinimumSize = new Size(330, 44),
                Font = new Font(Font, FontStyle.Bold),
                Padding = new Padding(12, 5, 12, 5)
            };
            _analyzeButton.Click += async (s, e) => await AnalyzeAsync();
            _cancelButton = new Button { Text = L10n.T("Cancel"), AutoSize = true, MinimumSize = new Size(90, 44), Enabled = false };
            _cancelButton.Click += (s, e) => _cts?.Cancel();
            _reportButton = new Button { Text = L10n.T("OpenReport"), AutoSize = true, MinimumSize = new Size(130, 44), Enabled = false };
            _reportButton.Click += (s, e) => OpenReport();
            actionBar.Controls.Add(_analyzeButton);
            actionBar.Controls.Add(_cancelButton);
            actionBar.Controls.Add(_reportButton);
            outer.Controls.Add(actionBar, 0, 4);

            _summaryLabel = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(1120, 0),
                Text = L10n.T("NoAnalysis"),
                Padding = new Padding(2, 5, 2, 5)
            };
            outer.Controls.Add(_summaryLabel, 0, 5);

            _resultsTabs = new TabControl { Dock = DockStyle.Fill, MinimumSize = new Size(0, 220) };
            _uniqueGrid = CreateGrid();
            _versionGrid = CreateGrid();
            _duplicateGrid = CreateGrid();
            _errorGrid = CreateGrid();
            AddResultTab(L10n.T("TabRecover"), _uniqueGrid);
            AddResultTab(L10n.T("TabVersions"), _versionGrid);
            AddResultTab(L10n.T("TabDuplicates"), _duplicateGrid);
            AddResultTab(L10n.T("TabProblems"), _errorGrid);
            outer.Controls.Add(_resultsTabs, 0, 6);

            var cleanupBox = new GroupBox
            {
                Text = L10n.T("CleanupGroup"),
                Dock = DockStyle.Fill,
                AutoSize = true,
                Padding = new Padding(10)
            };
            var cleanupLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 1,
                RowCount = 6
            };
            cleanupBox.Controls.Add(cleanupLayout);

            _quarantineRadio = new RadioButton
            {
                Text = L10n.T("Quarantine"),
                AutoSize = true,
                Checked = true,
                Font = new Font(Font, FontStyle.Bold),
                Margin = new Padding(3, 6, 3, 5)
            };
            _quarantineRadio.CheckedChanged += (s, e) =>
            {
                if (_quarantineRadio.Checked) _recycleRadio.Checked = false;
            };
            cleanupLayout.Controls.Add(_quarantineRadio, 0, 0);

            _otherOptionsLink = new LinkLabel
            {
                Text = L10n.T("OtherOptions"),
                AutoSize = true,
                Margin = new Padding(3, 2, 3, 4)
            };
            _otherOptionsLink.LinkClicked += (s, e) => ToggleOtherOptions();
            cleanupLayout.Controls.Add(_otherOptionsLink, 0, 1);

            _otherOptionsPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                Visible = false,
                Padding = new Padding(12, 2, 0, 4)
            };
            var optionsFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false
            };
            _recycleRadio = new RadioButton { Text = L10n.T("Recycle"), AutoSize = true };
            _recycleRadio.CheckedChanged += (s, e) =>
            {
                if (_recycleRadio.Checked) _quarantineRadio.Checked = false;
                else if (!_quarantineRadio.Checked) _quarantineRadio.Checked = true;
            };
            _removeEmptyCheck = new CheckBox { Text = L10n.T("RemoveEmpty"), AutoSize = true, Checked = true };
            optionsFlow.Controls.Add(_recycleRadio);
            optionsFlow.Controls.Add(_removeEmptyCheck);
            _otherOptionsPanel.Controls.Add(optionsFlow);
            cleanupLayout.Controls.Add(_otherOptionsPanel, 0, 2);

            _confirmCheck = new CheckBox
            {
                AutoSize = true,
                Text = L10n.T("Confirm"),
                Font = new Font(Font, FontStyle.Bold),
                Margin = new Padding(3, 8, 3, 6)
            };
            _confirmCheck.CheckedChanged += (s, e) => UpdateCleanupEnabled();
            cleanupLayout.Controls.Add(_confirmCheck, 0, 3);

            var cleanupButtons = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true };
            _cleanupButton = new Button
            {
                Text = L10n.T("Cleanup"),
                AutoSize = true,
                MinimumSize = new Size(320, 46),
                Enabled = false,
                Font = new Font(Font, FontStyle.Bold),
                Padding = new Padding(12, 5, 12, 5)
            };
            _cleanupButton.Click += async (s, e) => await CleanupAsync();
            _restoreButton = new Button
            {
                Text = L10n.T("Restore"),
                AutoSize = true,
                MinimumSize = new Size(180, 46)
            };
            _restoreButton.Click += async (s, e) => await RestoreQuarantineAsync();
            cleanupButtons.Controls.Add(_cleanupButton);
            cleanupButtons.Controls.Add(_restoreButton);
            cleanupLayout.Controls.Add(cleanupButtons, 0, 4);

            var invariant = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(1120, 0),
                Text = "🔒 " + L10n.T("ProtectedReminder"),
                Padding = new Padding(2, 4, 2, 2)
            };
            cleanupLayout.Controls.Add(invariant, 0, 5);
            outer.Controls.Add(cleanupBox, 0, 7);

            var statusPanel = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 1, RowCount = 2 };
            _progress = new ProgressBar { Dock = DockStyle.Fill, Height = 20, Style = ProgressBarStyle.Continuous };
            _statusLabel = new Label { AutoSize = true, Text = L10n.T("Ready") };
            statusPanel.Controls.Add(_progress, 0, 0);
            statusPanel.Controls.Add(_statusLabel, 0, 1);
            outer.Controls.Add(statusPanel, 0, 8);

            var privacy = new Label
            {
                AutoSize = true,
                MaximumSize = new Size(1120, 0),
                ForeColor = SystemColors.GrayText,
                Text = L10n.CurrentLanguage == AppLanguage.Italian
                    ? "Lavora in locale • Nessun upload • Nessun account • Nessun permesso amministrativo richiesto"
                    : "Works locally • No uploads • No account • No administrator rights requested"
            };
            outer.Controls.Add(privacy, 0, 9);

            BuildHelpPage(helpPage);
        }

        private Control CreateFolderPanel(
            string title,
            string badge,
            string description,
            Color background,
            Color badgeColor,
            out TextBox box,
            out Button browseButton,
            EventHandler browseHandler)
        {
            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                BackColor = background,
                Padding = new Padding(12),
                Margin = new Padding(0, 7, 0, 0)
            };
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 2, RowCount = 3 };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            panel.Controls.Add(layout);

            var header = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true };
            header.Controls.Add(new Label { AutoSize = true, Font = new Font(Font.FontFamily, 11F, FontStyle.Bold), Text = title, Margin = new Padding(0, 2, 10, 2) });
            header.Controls.Add(new Label
            {
                AutoSize = true,
                Font = new Font(Font.FontFamily, 8F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = badgeColor,
                Text = "  " + badge + "  ",
                Padding = new Padding(2)
            });
            layout.Controls.Add(header, 0, 0);
            layout.SetColumnSpan(header, 2);

            var desc = new Label { AutoSize = true, MaximumSize = new Size(1000, 0), Text = description, Margin = new Padding(0, 4, 0, 2) };
            layout.Controls.Add(desc, 0, 1);
            layout.SetColumnSpan(desc, 2);

            box = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(0, 6, 8, 0) };
            browseButton = new Button { Text = L10n.T("Browse"), AutoSize = true, Margin = new Padding(0, 4, 0, 0), MinimumSize = new Size(90, 28) };
            browseButton.Click += browseHandler;
            layout.Controls.Add(box, 0, 2);
            layout.Controls.Add(browseButton, 1, 2);
            return panel;
        }

        private DataGridView CreateGrid()
        {
            var grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoGenerateColumns = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = L10n.T("GridOld"), DataPropertyName = "Source", FillWeight = 42 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = L10n.T("GridSize"), DataPropertyName = "Size", FillWeight = 10 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = L10n.T("GridCurrent"), DataPropertyName = "Reference", FillWeight = 32 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = L10n.T("GridNote"), DataPropertyName = "Note", FillWeight = 30 });
            grid.CellDoubleClick += (s, e) => OpenSelectedSource((DataGridView)s, e.RowIndex);
            return grid;
        }

        private void AddResultTab(string title, DataGridView grid)
        {
            var page = new TabPage(title);
            page.Controls.Add(grid);
            _resultsTabs.TabPages.Add(page);
        }

        private void BuildHelpPage(TabPage helpPage)
        {
            var box = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BorderStyle = BorderStyle.None,
                BackColor = SystemColors.Window,
                Font = new Font("Segoe UI", 10F),
                Text = L10n.T("HelpText")
            };
            helpPage.Controls.Add(box);
        }

        private void ChangeLanguage(object sender, EventArgs e)
        {
            if (_busy) return;
            using (var dialog = new LanguageDialog(L10n.CurrentLanguage))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK || dialog.SelectedLanguage == L10n.CurrentLanguage) return;
                if (MessageBox.Show(this, L10n.T("LanguageRestart"), L10n.T("LanguageRestartTitle"), MessageBoxButtons.OKCancel, MessageBoxIcon.Information) != DialogResult.OK) return;
                SettingsStore.SaveLanguage(dialog.SelectedLanguage);
                Application.Restart();
            }
        }

        private void ToggleOtherOptions()
        {
            _otherOptionsPanel.Visible = !_otherOptionsPanel.Visible;
            _otherOptionsLink.Text = _otherOptionsPanel.Visible ? L10n.T("HideOptions") : L10n.T("OtherOptions");
        }

        private void BrowseSource(object sender, EventArgs e)
        {
            var p = ChooseFolder(_sourceBox.Text, L10n.T("ChooseOldDialog"));
            if (p != null) _sourceBox.Text = p;
        }

        private void BrowseReference(object sender, EventArgs e)
        {
            var p = ChooseFolder(_referenceBox.Text, L10n.T("ChooseCurrentDialog"));
            if (p != null) _referenceBox.Text = p;
        }

        private string ChooseFolder(string initial, string description)
        {
            using (var dlg = new FolderBrowserDialog { Description = description, ShowNewFolderButton = false })
            {
                if (!string.IsNullOrWhiteSpace(initial) && Directory.Exists(initial)) dlg.SelectedPath = initial;
                return dlg.ShowDialog(this) == DialogResult.OK ? dlg.SelectedPath : null;
            }
        }

        private void PathsChanged(object sender, EventArgs e)
        {
            if (_busy) return;
            _analysis = null;
            _confirmCheck.Checked = false;
            _lastReportPath = null;
            _reportButton.Enabled = false;
            ClearGrids();
            _summaryLabel.Text = L10n.T("PathsChanged");
            _cleanupButton.Text = L10n.T("Cleanup");
            UpdateCleanupEnabled();
        }

        private async Task AnalyzeAsync()
        {
            var error = PathSafety.ValidatePair(_sourceBox.Text, _referenceBox.Text);
            if (error != null)
            {
                MessageBox.Show(this, error, L10n.T("PathCheckTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var sourcePath = _sourceBox.Text;
            var referencePath = _referenceBox.Text;
            SetBusy(true);
            _analysis = null;
            _confirmCheck.Checked = false;
            ClearGrids();
            _cts = new CancellationTokenSource();
            try
            {
                var engine = new ComparisonEngine();
                var progress = new Progress<ProgressInfo>(UpdateProgress);
                _analysis = await Task.Run(() => engine.Analyze(sourcePath, referencePath, _cts.Token, p => ((IProgress<ProgressInfo>)progress).Report(p)));
                BindAnalysis(_analysis);
                _lastReportPath = ReportWriter.WriteAnalysisReport(_analysis);
                _reportButton.Enabled = true;
                _statusLabel.Text = L10n.T("AnalyzeDone");
                _progress.Value = 100;
            }
            catch (OperationCanceledException)
            {
                _statusLabel.Text = L10n.T("AnalyzeCanceled");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, L10n.T("AnalyzeErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                _statusLabel.Text = L10n.T("AnalyzeErrorStatus");
            }
            finally
            {
                SetBusy(false);
                UpdateCleanupEnabled();
            }
        }

        private async Task CleanupAsync()
        {
            if (_analysis == null || !_confirmCheck.Checked) return;

            var mode = _recycleRadio.Checked ? CleanupMode.RecycleBin : CleanupMode.Quarantine;
            var removeEmptyFolders = _removeEmptyCheck.Checked;
            var modeName = mode == CleanupMode.Quarantine ? L10n.T("ModeSafety") : L10n.T("ModeRecycle");
            var message = L10n.T(
                "CleanupConfirm",
                _analysis.SourceRoot,
                _analysis.ReferenceRoot,
                _analysis.ExactDuplicateCount.ToString("N0"),
                modeName);

            if (MessageBox.Show(this, message, L10n.T("CleanupConfirmTitle"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return;

            SetBusy(true);
            _cts = new CancellationTokenSource();
            try
            {
                var engine = new CleanupEngine();
                var progress = new Progress<ProgressInfo>(UpdateProgress);
                var cleanup = await Task.Run(() => engine.Clean(_analysis, mode, removeEmptyFolders, _cts.Token, p => ((IProgress<ProgressInfo>)progress).Report(p)));
                _lastQuarantineRoot = cleanup.QuarantineRoot;
                _statusLabel.Text = L10n.T("CleanupDoneStatus", cleanup.RemovedCount.ToString("N0"), cleanup.SkippedCount.ToString("N0"));
                _progress.Value = 100;

                var safetyPart = string.IsNullOrEmpty(cleanup.QuarantineRoot)
                    ? string.Empty
                    : L10n.T("CleanupSafetyLocation", cleanup.QuarantineRoot);
                var details = L10n.T("CleanupDone", cleanup.RemovedCount.ToString("N0"), cleanup.SkippedCount.ToString("N0"), safetyPart);
                MessageBox.Show(this, details, L10n.T("CleanupDoneTitle"), MessageBoxButtons.OK, cleanup.ErrorCount == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);

                _analysis = null;
                _confirmCheck.Checked = false;
                _cleanupButton.Text = L10n.T("Cleanup");
                UpdateCleanupEnabled();
            }
            catch (OperationCanceledException)
            {
                _statusLabel.Text = L10n.T("CleanupCanceled");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, L10n.T("CleanupErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
                _statusLabel.Text = L10n.T("CleanupErrorStatus");
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async Task RestoreQuarantineAsync()
        {
            string root = null;
            if (!string.IsNullOrEmpty(_lastQuarantineRoot) && Directory.Exists(_lastQuarantineRoot))
            {
                var useLast = MessageBox.Show(this, L10n.T("RestoreLast", _lastQuarantineRoot), L10n.T("RestoreTitle"), MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (useLast == DialogResult.Cancel) return;
                if (useLast == DialogResult.Yes) root = _lastQuarantineRoot;
            }
            if (root == null)
            {
                root = ChooseFolder(string.Empty, L10n.T("RestoreChoose"));
                if (root == null) return;
            }

            if (MessageBox.Show(this, L10n.T("RestoreConfirm"), L10n.T("RestoreConfirmTitle"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return;

            SetBusy(true);
            _cts = new CancellationTokenSource();
            try
            {
                var engine = new CleanupEngine();
                var progress = new Progress<ProgressInfo>(UpdateProgress);
                var restored = await Task.Run(() => engine.RestoreQuarantine(root, _cts.Token, p => ((IProgress<ProgressInfo>)progress).Report(p)));
                _progress.Value = 100;
                _statusLabel.Text = L10n.T("RestoreDoneStatus", restored.RestoredCount.ToString("N0"), restored.ConflictCount.ToString("N0"));
                MessageBox.Show(this,
                    L10n.T("RestoreDone", restored.RestoredCount.ToString("N0"), restored.ConflictCount.ToString("N0"), restored.ErrorCount.ToString("N0")),
                    L10n.T("RestoreTitle"),
                    MessageBoxButtons.OK,
                    restored.ErrorCount == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
            catch (OperationCanceledException)
            {
                _statusLabel.Text = L10n.T("RestoreCanceled");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, L10n.T("RestoreErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private void BindAnalysis(AnalysisResult result)
        {
            var unique = result.Items.Where(x => x.Kind == ComparisonKind.Unique).Select(ToGridRow).ToList();
            var versions = result.Items.Where(x => x.Kind == ComparisonKind.PossibleVersion).Select(ToGridRow).ToList();
            var duplicates = result.Items.Where(x => x.Kind == ComparisonKind.ExactDuplicate).Select(ToGridRow).ToList();
            var errors = result.Items.Where(x => x.Kind == ComparisonKind.Error).Select(ToGridRow).ToList();
            foreach (var e in result.ScanErrors)
                errors.Add(new ResultGridRow { Source = e.Path, Size = string.Empty, Reference = string.Empty, Note = e.Message });

            _uniqueGrid.DataSource = unique;
            _versionGrid.DataSource = versions;
            _duplicateGrid.DataSource = duplicates;
            _errorGrid.DataSource = errors;

            _resultsTabs.TabPages[0].Text = L10n.T("TabRecover") + " (" + result.UniqueCount.ToString("N0") + ")";
            _resultsTabs.TabPages[1].Text = L10n.T("TabVersions") + " (" + result.PossibleVersionCount.ToString("N0") + ")";
            _resultsTabs.TabPages[2].Text = L10n.T("TabDuplicates") + " (" + result.ExactDuplicateCount.ToString("N0") + ")";
            _resultsTabs.TabPages[3].Text = L10n.T("TabProblems") + " (" + result.ErrorCount.ToString("N0") + ")";
            _resultsTabs.SelectedIndex = result.UniqueCount > 0 ? 0 : (result.PossibleVersionCount > 0 ? 1 : 2);

            _summaryLabel.Text = L10n.T(
                "Summary",
                result.SourceFileCount.ToString("N0"),
                result.ReferenceFileCount.ToString("N0"),
                result.ExactDuplicateCount.ToString("N0"),
                FormatBytes(result.DuplicateBytes),
                result.UniqueCount.ToString("N0"),
                result.PossibleVersionCount.ToString("N0"),
                result.ErrorCount.ToString("N0"));

            _cleanupButton.Text = L10n.T("CleanupCount", result.ExactDuplicateCount.ToString("N0"));
        }

        private ResultGridRow ToGridRow(ComparisonItem item)
        {
            return new ResultGridRow
            {
                Source = item.Source?.RelativePath ?? item.Source?.FullPath ?? string.Empty,
                SourceFullPath = item.Source?.FullPath,
                Size = item.Source == null ? string.Empty : FormatBytes(item.Source.Length),
                Reference = item.ReferencePath ?? string.Empty,
                Note = item.Note ?? string.Empty
            };
        }

        private void ClearGrids()
        {
            if (_uniqueGrid == null) return;
            _uniqueGrid.DataSource = null;
            _versionGrid.DataSource = null;
            _duplicateGrid.DataSource = null;
            _errorGrid.DataSource = null;
            _resultsTabs.TabPages[0].Text = L10n.T("TabRecover");
            _resultsTabs.TabPages[1].Text = L10n.T("TabVersions");
            _resultsTabs.TabPages[2].Text = L10n.T("TabDuplicates");
            _resultsTabs.TabPages[3].Text = L10n.T("TabProblems");
        }

        private void UpdateProgress(ProgressInfo p)
        {
            if (p.Total > 0)
            {
                _progress.Style = ProgressBarStyle.Continuous;
                var percent = Math.Max(0, Math.Min(100, (int)((long)p.Current * 100L / p.Total)));
                _progress.Value = percent;
            }
            else
            {
                _progress.Style = ProgressBarStyle.Marquee;
            }
            _statusLabel.Text = p.Phase + (string.IsNullOrEmpty(p.CurrentPath) ? string.Empty : " — " + Shorten(p.CurrentPath, 100));
        }

        private void SetBusy(bool busy)
        {
            _busy = busy;
            _analyzeButton.Enabled = !busy;
            _sourceBox.Enabled = !busy;
            _referenceBox.Enabled = !busy;
            _sourceBrowseButton.Enabled = !busy;
            _referenceBrowseButton.Enabled = !busy;
            _languageButton.Enabled = !busy;
            _cleanupButton.Enabled = false;
            _restoreButton.Enabled = !busy;
            _cancelButton.Enabled = busy;
            if (busy)
            {
                _progress.Style = ProgressBarStyle.Marquee;
                _progress.MarqueeAnimationSpeed = 25;
            }
            else
            {
                _progress.MarqueeAnimationSpeed = 0;
                _progress.Style = ProgressBarStyle.Continuous;
            }
        }

        private void UpdateCleanupEnabled()
        {
            _cleanupButton.Enabled = !_busy && _analysis != null && _analysis.ExactDuplicateCount > 0 && _confirmCheck.Checked;
        }

        private void OpenReport()
        {
            if (string.IsNullOrEmpty(_lastReportPath) || !File.Exists(_lastReportPath)) return;
            try { Process.Start("explorer.exe", "/select,\"" + _lastReportPath + "\""); } catch { }
        }

        private void OpenSelectedSource(DataGridView grid, int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= grid.Rows.Count) return;
            var row = grid.Rows[rowIndex].DataBoundItem as ResultGridRow;
            if (row == null || string.IsNullOrEmpty(row.SourceFullPath) || !File.Exists(row.SourceFullPath)) return;
            try { Process.Start("explorer.exe", "/select,\"" + row.SourceFullPath + "\""); } catch { }
        }

        private static string FormatBytes(long bytes)
        {
            string[] units = { "B", "KB", "MB", "GB", "TB" };
            double value = bytes;
            int unit = 0;
            while (value >= 1024 && unit < units.Length - 1)
            {
                value /= 1024;
                unit++;
            }
            return value.ToString(unit == 0 ? "N0" : "N2") + " " + units[unit];
        }

        private static string Shorten(string value, int max)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= max) return value;
            return "…" + value.Substring(value.Length - max + 1);
        }

        private sealed class ResultGridRow
        {
            public string Source { get; set; }
            public string SourceFullPath { get; set; }
            public string Size { get; set; }
            public string Reference { get; set; }
            public string Note { get; set; }
        }
    }
}
