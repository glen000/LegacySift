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
        private Label _cleanupExplanationLabel;
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
            MinimumSize = new Size(920, 680);
            Size = new Size(1160, 780);
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
                Padding = new Padding(14, 8, 14, 7),
                BackColor = SystemColors.ControlLightLight
            };
            header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            header.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var title = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                Text = "LegacySift"
            };
            _languageButton = new Button
            {
                Text = L10n.T("LanguageButton"),
                AutoSize = true,
                Margin = new Padding(10, 3, 0, 0)
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

            BuildWorkPage(workPage);
            BuildHelpPage(helpPage);
        }

        private void BuildWorkPage(TabPage workPage)
        {
            // The primary workflow deliberately has no vertical scrolling.
            // The cleanup action must remain visible in a normal-size window.
            var outer = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 6,
                Padding = new Padding(12)
            };
            outer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            outer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            outer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            outer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            outer.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            outer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            workPage.Controls.Add(outer);

            var intro = new Label
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                Font = new Font(Font.FontFamily, 9.5F, FontStyle.Bold),
                Text = L10n.T("Intro"),
                Padding = new Padding(2, 0, 2, 6)
            };
            outer.Controls.Add(intro, 0, 0);

            var folderPair = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                Height = 108,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0),
                GrowStyle = TableLayoutPanelGrowStyle.FixedSize
            };
            folderPair.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            folderPair.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            folderPair.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));

            var oldPanel = CreateFolderPanel(
                L10n.T("OldTitle"),
                L10n.T("OldBadge"),
                L10n.T("OldDescription"),
                Color.FromArgb(255, 248, 225),
                Color.FromArgb(129, 77, 0),
                out _sourceBox,
                out _sourceBrowseButton,
                BrowseSource);
            oldPanel.Margin = new Padding(0, 0, 6, 0);

            var currentPanel = CreateFolderPanel(
                L10n.T("CurrentTitle"),
                L10n.T("CurrentBadge"),
                L10n.T("CurrentDescription"),
                Color.FromArgb(232, 245, 233),
                Color.FromArgb(27, 94, 32),
                out _referenceBox,
                out _referenceBrowseButton,
                BrowseReference);
            currentPanel.Margin = new Padding(6, 0, 0, 0);

            folderPair.Controls.Add(oldPanel, 0, 0);
            folderPair.Controls.Add(currentPanel, 1, 0);
            outer.Controls.Add(folderPair, 0, 1);

            _sourceBox.TextChanged += PathsChanged;
            _referenceBox.TextChanged += PathsChanged;

            var checkArea = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 1,
                RowCount = 3,
                Margin = new Padding(0, 4, 0, 2)
            };
            checkArea.Controls.Add(new Label
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                Text = L10n.T("Direction"),
                Font = new Font(Font, FontStyle.Bold),
                Padding = new Padding(2, 2, 2, 2)
            }, 0, 0);

            var actionBar = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(0, 2, 0, 2),
                Margin = new Padding(0)
            };
            _analyzeButton = new Button
            {
                Text = L10n.T("Analyze"),
                AutoSize = true,
                MinimumSize = new Size(310, 40),
                Font = new Font(Font, FontStyle.Bold),
                Padding = new Padding(10, 4, 10, 4)
            };
            _analyzeButton.Click += async (s, e) => await AnalyzeAsync();
            _cancelButton = new Button { Text = L10n.T("Cancel"), AutoSize = true, MinimumSize = new Size(90, 40), Enabled = false };
            _cancelButton.Click += (s, e) => _cts?.Cancel();
            _reportButton = new Button { Text = L10n.T("OpenReport"), AutoSize = true, MinimumSize = new Size(130, 40), Enabled = false };
            _reportButton.Click += (s, e) => OpenReport();
            actionBar.Controls.Add(_analyzeButton);
            actionBar.Controls.Add(_cancelButton);
            actionBar.Controls.Add(_reportButton);
            checkArea.Controls.Add(actionBar, 0, 1);

            var progressArea = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 1, RowCount = 2, Margin = new Padding(0) };
            _progress = new ProgressBar { Dock = DockStyle.Fill, Height = 12, Style = ProgressBarStyle.Continuous, Margin = new Padding(0, 1, 0, 1) };
            _statusLabel = new Label { AutoSize = true, Text = L10n.T("Ready"), ForeColor = SystemColors.GrayText, Margin = new Padding(0) };
            progressArea.Controls.Add(_progress, 0, 0);
            progressArea.Controls.Add(_statusLabel, 0, 1);
            checkArea.Controls.Add(progressArea, 0, 2);
            outer.Controls.Add(checkArea, 0, 2);

            var summaryPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                BackColor = Color.FromArgb(245, 247, 250),
                Padding = new Padding(9, 6, 9, 6),
                Margin = new Padding(0, 2, 0, 5)
            };
            _summaryLabel = new Label
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                Text = L10n.T("NoAnalysis"),
                Font = new Font(Font.FontFamily, 9F, FontStyle.Bold)
            };
            summaryPanel.Controls.Add(_summaryLabel);
            outer.Controls.Add(summaryPanel, 0, 3);

            _resultsTabs = new TabControl { Dock = DockStyle.Fill, MinimumSize = new Size(0, 125), Margin = new Padding(0) };
            _uniqueGrid = CreateGrid();
            _versionGrid = CreateGrid();
            _duplicateGrid = CreateGrid();
            _errorGrid = CreateGrid();
            AddResultTab(L10n.T("TabRecover"), L10n.T("TabRecoverHelp"), _uniqueGrid);
            AddResultTab(L10n.T("TabVersions"), L10n.T("TabVersionsHelp"), _versionGrid);
            AddResultTab(L10n.T("TabDuplicates"), L10n.T("TabDuplicatesHelp"), _duplicateGrid);
            AddResultTab(L10n.T("TabProblems"), L10n.T("TabProblemsHelp"), _errorGrid);
            outer.Controls.Add(_resultsTabs, 0, 4);

            outer.Controls.Add(CreateCleanupPanel(), 0, 5);
        }

        private Control CreateCleanupPanel()
        {
            var cleanupBox = new GroupBox
            {
                Text = L10n.T("CleanupGroup"),
                Dock = DockStyle.Fill,
                AutoSize = true,
                Padding = new Padding(9, 7, 9, 7),
                Margin = new Padding(0, 6, 0, 0)
            };

            var cleanupLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                ColumnCount = 1,
                RowCount = 5,
                Margin = new Padding(0)
            };

            _cleanupExplanationLabel = new Label
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                Text = L10n.T("CleanupBeforeAnalysis"),
                Font = new Font(Font.FontFamily, 9F, FontStyle.Bold),
                Padding = new Padding(2, 0, 2, 3)
            };
            cleanupLayout.Controls.Add(_cleanupExplanationLabel, 0, 0);

            var modeRow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Margin = new Padding(0)
            };
            _quarantineRadio = new RadioButton
            {
                Text = L10n.T("Quarantine"),
                AutoSize = true,
                Checked = true,
                Font = new Font(Font, FontStyle.Bold),
                Margin = new Padding(3, 3, 12, 3)
            };
            _quarantineRadio.CheckedChanged += (s, e) =>
            {
                if (_quarantineRadio.Checked) _recycleRadio.Checked = false;
            };
            modeRow.Controls.Add(_quarantineRadio);

            _otherOptionsLink = new LinkLabel
            {
                Text = L10n.T("OtherOptions"),
                AutoSize = true,
                Margin = new Padding(3, 5, 6, 3)
            };
            _otherOptionsLink.LinkClicked += (s, e) => ToggleOtherOptions();
            modeRow.Controls.Add(_otherOptionsLink);

            _otherOptionsPanel = new Panel
            {
                AutoSize = true,
                Visible = false,
                Margin = new Padding(0)
            };
            var optionsFlow = new FlowLayoutPanel
            {
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Margin = new Padding(0)
            };
            _recycleRadio = new RadioButton { Text = L10n.T("Recycle"), AutoSize = true, Margin = new Padding(6, 3, 12, 3) };
            _recycleRadio.CheckedChanged += (s, e) =>
            {
                if (_recycleRadio.Checked) _quarantineRadio.Checked = false;
                else if (!_quarantineRadio.Checked) _quarantineRadio.Checked = true;
            };
            _removeEmptyCheck = new CheckBox { Text = L10n.T("RemoveEmpty"), AutoSize = true, Checked = true, Margin = new Padding(6, 3, 3, 3) };
            optionsFlow.Controls.Add(_recycleRadio);
            optionsFlow.Controls.Add(_removeEmptyCheck);
            _otherOptionsPanel.Controls.Add(optionsFlow);
            modeRow.Controls.Add(_otherOptionsPanel);
            cleanupLayout.Controls.Add(modeRow, 0, 1);

            _confirmCheck = new CheckBox
            {
                AutoSize = true,
                Text = L10n.T("Confirm"),
                Font = new Font(Font, FontStyle.Bold),
                Margin = new Padding(3, 1, 3, 3),
                Enabled = false
            };
            _confirmCheck.CheckedChanged += (s, e) => UpdateCleanupEnabled();
            cleanupLayout.Controls.Add(_confirmCheck, 0, 2);

            var protectedReminder = new Label
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                Text = L10n.T("ProtectedReminder"),
                ForeColor = Color.FromArgb(27, 94, 32),
                Font = new Font(Font, FontStyle.Bold),
                Margin = new Padding(3, 2, 3, 4)
            };
            cleanupLayout.Controls.Add(protectedReminder, 0, 3);

            var cleanupButtons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Margin = new Padding(0)
            };
            _cleanupButton = new Button
            {
                Text = L10n.T("Cleanup"),
                AutoSize = true,
                MinimumSize = new Size(340, 42),
                Enabled = false,
                Font = new Font(Font, FontStyle.Bold),
                Padding = new Padding(10, 4, 10, 4)
            };
            _cleanupButton.Click += async (s, e) => await CleanupAsync();
            _restoreButton = new Button
            {
                Text = L10n.T("Restore"),
                AutoSize = true,
                MinimumSize = new Size(170, 42),
                Margin = new Padding(8, 0, 0, 0)
            };
            _restoreButton.Click += async (s, e) => await RestoreQuarantineAsync();
            cleanupButtons.Controls.Add(_cleanupButton);
            cleanupButtons.Controls.Add(_restoreButton);
            cleanupLayout.Controls.Add(cleanupButtons, 0, 4);

            cleanupBox.Controls.Add(cleanupLayout);
            return cleanupBox;
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
                AutoSize = false,
                BackColor = background,
                Padding = new Padding(10),
                MinimumSize = new Size(0, 105)
            };
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                ColumnCount = 2,
                RowCount = 3,
                GrowStyle = TableLayoutPanelGrowStyle.FixedSize
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            panel.Controls.Add(layout);

            var header = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true, Margin = new Padding(0) };
            header.Controls.Add(new Label { AutoSize = true, Font = new Font(Font.FontFamily, 10.5F, FontStyle.Bold), Text = title, Margin = new Padding(0, 2, 9, 2) });
            header.Controls.Add(new Label
            {
                AutoSize = true,
                Font = new Font(Font.FontFamily, 7.8F, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = badgeColor,
                Text = "  " + badge + "  ",
                Padding = new Padding(2),
                Margin = new Padding(0, 2, 0, 0)
            });
            layout.Controls.Add(header, 0, 0);
            layout.SetColumnSpan(header, 2);

            var desc = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                Text = description,
                Margin = new Padding(0, 3, 0, 1),
                TextAlign = ContentAlignment.TopLeft
            };
            layout.Controls.Add(desc, 0, 1);
            layout.SetColumnSpan(desc, 2);

            box = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(0, 5, 7, 0) };
            browseButton = new Button { Text = L10n.T("Browse"), AutoSize = true, Margin = new Padding(0, 3, 0, 0), MinimumSize = new Size(86, 28) };
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
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                BackgroundColor = SystemColors.Window
            };
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = L10n.T("GridOld"), DataPropertyName = "Source", FillWeight = 42 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = L10n.T("GridSize"), DataPropertyName = "Size", FillWeight = 10 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = L10n.T("GridCurrent"), DataPropertyName = "Reference", FillWeight = 32 });
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = L10n.T("GridNote"), DataPropertyName = "Note", FillWeight = 30 });
            grid.CellDoubleClick += (s, e) => OpenSelectedSource((DataGridView)s, e.RowIndex);
            return grid;
        }

        private void AddResultTab(string title, string explanation, DataGridView grid)
        {
            var page = new TabPage(title);
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(4) };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            layout.Controls.Add(new Label
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                Text = explanation,
                Padding = new Padding(4, 2, 4, 5),
                ForeColor = SystemColors.GrayText
            }, 0, 0);
            layout.Controls.Add(grid, 0, 1);
            page.Controls.Add(layout);
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
                L10n.CurrentLanguage = dialog.SelectedLanguage;
                SettingsStore.SaveLanguage(dialog.SelectedLanguage);
                MessageBox.Show(this, L10n.T("LanguageRestart"), L10n.T("LanguageRestartTitle"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                Application.Restart();
            }
        }

        private void BrowseSource(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog { Description = L10n.T("ChooseOldDialog"), ShowNewFolderButton = false })
            {
                if (dialog.ShowDialog(this) == DialogResult.OK) _sourceBox.Text = dialog.SelectedPath;
            }
        }

        private void BrowseReference(object sender, EventArgs e)
        {
            using (var dialog = new FolderBrowserDialog { Description = L10n.T("ChooseCurrentDialog"), ShowNewFolderButton = false })
            {
                if (dialog.ShowDialog(this) == DialogResult.OK) _referenceBox.Text = dialog.SelectedPath;
            }
        }

        private void PathsChanged(object sender, EventArgs e)
        {
            if (_analysis == null) return;
            _analysis = null;
            _confirmCheck.Checked = false;
            _confirmCheck.Enabled = false;
            _reportButton.Enabled = !string.IsNullOrEmpty(_lastReportPath) && File.Exists(_lastReportPath);
            _summaryLabel.Text = L10n.T("PathsChanged");
            _cleanupExplanationLabel.Text = L10n.T("CleanupBeforeAnalysis");
            ClearGrids();
            UpdateCleanupEnabled();
        }

        private async Task AnalyzeAsync()
        {
            var error = PathSafety.ValidatePair(_sourceBox.Text, _referenceBox.Text);
            if (!string.IsNullOrEmpty(error))
            {
                MessageBox.Show(this, error, L10n.T("PathCheckTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            SetBusy(true);
            _analysis = null;
            _confirmCheck.Checked = false;
            _confirmCheck.Enabled = false;
            ClearGrids();
            _summaryLabel.Text = L10n.T("CheckingNow");
            _cleanupExplanationLabel.Text = L10n.T("CleanupBeforeAnalysis");
            _cts = new CancellationTokenSource();
            var progress = new Progress<ProgressInfo>(UpdateProgress);

            try
            {
                var engine = new ComparisonEngine();
                var result = await Task.Run(() => engine.Analyze(_sourceBox.Text, _referenceBox.Text, _cts.Token, p => ((IProgress<ProgressInfo>)progress).Report(p)));
                _analysis = result;
                BindAnalysis(result);
                _lastReportPath = ReportWriter.WriteAnalysis(result);
                _reportButton.Enabled = true;
                _confirmCheck.Enabled = result.ExactDuplicateCount > 0;
                _statusLabel.Text = L10n.T("AnalyzeDone");
                _progress.Value = 100;
            }
            catch (OperationCanceledException)
            {
                _statusLabel.Text = L10n.T("AnalyzeCanceled");
            }
            catch (Exception ex)
            {
                _statusLabel.Text = L10n.T("AnalyzeErrorStatus");
                MessageBox.Show(this, ex.Message, L10n.T("AnalyzeErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetBusy(false);
                UpdateCleanupEnabled();
            }
        }

        private async Task CleanupAsync()
        {
            if (_analysis == null || _analysis.ExactDuplicateCount == 0) return;
            if (!_confirmCheck.Checked) return;

            var mode = _recycleRadio.Checked ? CleanupMode.RecycleBin : CleanupMode.Quarantine;
            var confirm = L10n.T(
                "CleanupConfirm",
                _analysis.SourceRoot,
                _analysis.ReferenceRoot,
                _analysis.ExactDuplicateCount.ToString("N0"),
                _analysis.UniqueCount.ToString("N0"),
                _analysis.PossibleVersionCount.ToString("N0"),
                _analysis.ErrorCount.ToString("N0"),
                mode == CleanupMode.Quarantine ? L10n.T("ModeSafety") : L10n.T("ModeRecycle"));

            if (MessageBox.Show(this, confirm, L10n.T("CleanupConfirmTitle"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return;

            SetBusy(true);
            _cts = new CancellationTokenSource();
            var progress = new Progress<ProgressInfo>(UpdateProgress);

            try
            {
                var result = await Task.Run(() => CleanupEngine.Cleanup(
                    _analysis,
                    mode,
                    _removeEmptyCheck.Checked,
                    _cts.Token,
                    p => ((IProgress<ProgressInfo>)progress).Report(p)));

                _lastReportPath = ReportWriter.WriteCleanup(result);
                _reportButton.Enabled = true;
                if (!string.IsNullOrEmpty(result.QuarantineRoot)) _lastQuarantineRoot = result.QuarantineRoot;

                _statusLabel.Text = L10n.T("CleanupDoneStatus", result.RemovedCount, result.SkippedCount);
                var extra = string.IsNullOrEmpty(result.QuarantineRoot)
                    ? string.Empty
                    : L10n.T("CleanupSafetyLocation", result.QuarantineRoot);
                MessageBox.Show(this,
                    L10n.T("CleanupDone", result.RemovedCount, result.SkippedCount, extra),
                    L10n.T("CleanupDoneTitle"),
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);

                _analysis = null;
                _confirmCheck.Checked = false;
                _confirmCheck.Enabled = false;
                _summaryLabel.Text = L10n.T("RunAgainAfterCleanup");
                _cleanupExplanationLabel.Text = L10n.T("CleanupBeforeAnalysis");
                ClearGrids();
            }
            catch (OperationCanceledException)
            {
                _statusLabel.Text = L10n.T("CleanupCanceled");
            }
            catch (Exception ex)
            {
                _statusLabel.Text = L10n.T("CleanupErrorStatus");
                MessageBox.Show(this, ex.Message, L10n.T("CleanupErrorTitle"), MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetBusy(false);
                UpdateCleanupEnabled();
            }
        }

        private async Task RestoreQuarantineAsync()
        {
            string selected = null;
            if (!string.IsNullOrEmpty(_lastQuarantineRoot) && Directory.Exists(_lastQuarantineRoot))
            {
                if (MessageBox.Show(this, L10n.T("RestoreLast", _lastQuarantineRoot), L10n.T("RestoreTitle"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    selected = _lastQuarantineRoot;
            }

            if (selected == null)
            {
                using (var dialog = new FolderBrowserDialog { Description = L10n.T("RestoreChoose"), ShowNewFolderButton = false })
                {
                    if (dialog.ShowDialog(this) != DialogResult.OK) return;
                    selected = dialog.SelectedPath;
                }
            }

            if (MessageBox.Show(this, L10n.T("RestoreConfirm"), L10n.T("RestoreConfirmTitle"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes)
                return;

            SetBusy(true);
            _cts = new CancellationTokenSource();
            var progress = new Progress<ProgressInfo>(UpdateProgress);
            try
            {
                var result = await Task.Run(() => CleanupEngine.Restore(selected, _cts.Token, p => ((IProgress<ProgressInfo>)progress).Report(p)));
                _statusLabel.Text = L10n.T("RestoreDoneStatus", result.RestoredCount, result.ConflictCount);
                MessageBox.Show(this,
                    L10n.T("RestoreDone", result.RestoredCount, result.ConflictCount, result.ErrorCount),
                    L10n.T("RestoreTitle"),
                    MessageBoxButtons.OK,
                    result.ErrorCount == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
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

            _cleanupExplanationLabel.Text = L10n.T(
                "CleanupAfterAnalysis",
                result.ExactDuplicateCount.ToString("N0"),
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

        private void ToggleOtherOptions()
        {
            _otherOptionsPanel.Visible = !_otherOptionsPanel.Visible;
            _otherOptionsLink.Text = _otherOptionsPanel.Visible ? L10n.T("HideOptions") : L10n.T("OtherOptions");
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
