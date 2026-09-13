using LegacySift.Core;
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
        OtherOptionsExpanded
    }

    internal sealed partial class MainForm
    {
        internal void PrepareLayoutTest(LayoutTestState state, Size clientSize, float scaleFactor)
        {
            _layoutTestMode = true;
            CreateControl();
            Show();
            Application.DoEvents();

            SuspendLayout();
            if (scaleFactor > 1.001F)
                Scale(new SizeF(scaleFactor, scaleFactor));
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
        }

        internal Bitmap CaptureLayoutTestImage()
        {
            var image = new Bitmap(Width, Height);
            DrawToBitmap(image, new Rectangle(Point.Empty, Size));
            return image;
        }

        private static AnalysisResult CreateLayoutTestAnalysis()
        {
            var result = new AnalysisResult
            {
                SourceRoot = @"C:\Recovered backup\Documents and family archive",
                ReferenceRoot = @"D:\Current protected files\Documents",
                SourceFileCount = 4,
                ReferenceFileCount = 3
            };
            result.Items.Add(LayoutItem(ComparisonKind.Unique, "Family photos\\holiday-archive-very-long-name.jpg", null, 48234496, L10n.T("UniqueNote")));
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
    }
}
