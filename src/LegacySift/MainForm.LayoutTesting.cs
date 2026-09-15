using LegacySift.Core;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace LegacySift
{
    internal enum LayoutTestState
    {
        Initial,
        FoldersSelected,
        AnalysisCompleted,
        CleanupReady,
        CleanupConfirmed,
        OtherOptionsExpanded
    }

    internal sealed partial class MainForm
    {
        internal void PrepareLayoutTest(LayoutTestState state, Size clientSize, float scaleFactor)
        {
            _layoutTestMode = true;
            // Make the regression matrix deterministic on hosted runners. The
            // interactive application remains DPI-aware; the harness supplies
            // the physical client size and simulates DPI text pressure without
            // depending on the runner's virtualized device DPI.
            AutoScaleMode = AutoScaleMode.None;
            CreateControl();
            Show();
            Application.DoEvents();

            SuspendLayout();
            if (System.Math.Abs(scaleFactor - 1F) > 0.001F)
            {
                ScaleFonts(this, scaleFactor);
                ScaleAbsoluteTableStyles(this, scaleFactor);
                // Fixed-height wrapping labels are automatically scaled by the
                // interactive DPI-aware form. AutoScale is disabled in this
                // deterministic harness, so reproduce that part explicitly.
                _cleanupExplanationLabel.Height = (int)System.Math.Ceiling(_cleanupExplanationLabel.Height * scaleFactor);
            }
            ClientSize = clientSize;

            if ((int)state >= (int)LayoutTestState.FoldersSelected)
            {
                _sourceBox.Text = @"C:\Recovered backup\Documents and family archive";
                _referenceBox.Text = @"D:\Current protected files\Documents";
            }

            if ((int)state >= (int)LayoutTestState.AnalysisCompleted)
            {
                var result = CreateLayoutTestAnalysis();
                _analysis = result;
                BindAnalysis(result);
                _statusLabel.Text = L10n.T("AnalyzeDone");
            }

            if ((int)state >= (int)LayoutTestState.CleanupReady)
            {
                _confirmCheck.Enabled = true;
                UpdateCleanupEnabled();
            }

            if ((int)state >= (int)LayoutTestState.CleanupConfirmed)
            {
                _confirmCheck.Checked = true;
                UpdateCleanupEnabled();
            }

            if ((int)state >= (int)LayoutTestState.OtherOptionsExpanded && !_otherOptionsPanel.Visible)
                ToggleOtherOptions();

            ResumeLayout(true);
            Application.DoEvents();
            PerformLayoutTree(this);
            PerformLayout();
            Application.DoEvents();

            var mainTabs = Controls.Find("MainTabs", true)[0] as TabControl;
            if (mainTabs != null && mainTabs.TabPages.Count > 1)
            {
                mainTabs.SelectedIndex = 1;
                Application.DoEvents();
                PerformLayoutTree(mainTabs.TabPages[1]);
                mainTabs.SelectedIndex = 0;
                Application.DoEvents();
                PerformLayoutTree(mainTabs.TabPages[0]);
            }
        }

        internal Bitmap CaptureLayoutTestImage()
        {
            var root = Controls["RootLayout"];
            var image = new Bitmap(root.ClientSize.Width, root.ClientSize.Height);
            root.DrawToBitmap(image, new Rectangle(Point.Empty, root.ClientSize));
            return image;
        }

        private static AnalysisResult CreateLayoutTestAnalysis()
        {
            var result = new AnalysisResult
            {
                SourceRoot = @"C:\Recovered backup\Documents and family archive",
                ReferenceRoot = @"D:\Current protected files\Documents",
                SourceFileCount = 7,
                ReferenceFileCount = 3
            };
            result.Items.Add(LayoutItem(ComparisonKind.Unique, "Family photos\\holiday-archive-very-long-name.jpg", null, 48234496, L10n.T("UniqueNote")));
            result.Items.Add(LayoutItem(ComparisonKind.Unique, "Family photos\\school-trip.png", null, 7340032, L10n.T("UniqueNote")));
            result.Items.Add(LayoutItem(ComparisonKind.Unique, "Documents\\old-tax-return.pdf", null, 524288, L10n.T("UniqueNote")));
            result.Items.Add(LayoutItem(ComparisonKind.Unique, "Desktop\\notes-from-old-computer.txt", null, 4096, L10n.T("UniqueNote")));
            result.Items.Add(LayoutItem(ComparisonKind.PossibleVersion, "Contracts\\contract.docx", @"D:\Current protected files\Documents\Contracts\contract.docx", 93841, L10n.T("VersionNoteOne")));
            result.Items.Add(LayoutItem(ComparisonKind.ExactDuplicate, "Music\\recording.wav", @"D:\Current protected files\Documents\Audio\recording-copy.wav", 157286400, L10n.T("ExactNote")));
            result.Items.Add(LayoutItem(ComparisonKind.Error, "Unreadable\\locked-file.bin", null, 1024, L10n.T("ReparseFile")));
            return result;
        }

        private static ComparisonItem LayoutItem(ComparisonKind kind, string relativePath, string referencePath, long length, string note)
        {
            return new ComparisonItem
            {
                Kind = kind,
                Source = new FileRecord
                {
                    FullPath = @"C:\Recovered backup\Documents and family archive\" + relativePath,
                    RelativePath = relativePath,
                    Name = System.IO.Path.GetFileName(relativePath),
                    Length = length
                },
                ReferencePath = referencePath,
                Note = note
            };
        }

        private static void PerformLayoutTree(Control control)
        {
            control.PerformLayout();
            foreach (Control child in control.Controls)
                PerformLayoutTree(child);
        }

        private static void ScaleFonts(Control root, float factor)
        {
            var fonts = new List<KeyValuePair<Control, Font>>();
            CollectFonts(root, fonts);
            foreach (var pair in fonts)
            {
                var font = pair.Value;
                pair.Key.Font = new Font(
                    font.FontFamily,
                    font.Size * factor,
                    font.Style,
                    font.Unit,
                    font.GdiCharSet,
                    font.GdiVerticalFont);
            }
        }

        private static void CollectFonts(Control control, List<KeyValuePair<Control, Font>> fonts)
        {
            fonts.Add(new KeyValuePair<Control, Font>(control, control.Font));
            foreach (Control child in control.Controls)
                CollectFonts(child, fonts);
        }

        private static void ScaleAbsoluteTableStyles(Control control, float factor)
        {
            var table = control as TableLayoutPanel;
            if (table != null)
            {
                foreach (ColumnStyle style in table.ColumnStyles)
                    if (style.SizeType == SizeType.Absolute)
                        style.Width *= factor;
                foreach (RowStyle style in table.RowStyles)
                    if (style.SizeType == SizeType.Absolute)
                        style.Height *= factor;
            }

            foreach (Control child in control.Controls)
                ScaleAbsoluteTableStyles(child, factor);
        }
    }
}
