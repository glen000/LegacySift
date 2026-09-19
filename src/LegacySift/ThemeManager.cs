using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace LegacySift
{
    internal sealed class ThemePalette
    {
        public static readonly ThemePalette Light = new ThemePalette(
            Color.FromArgb(245, 247, 250), Color.White, Color.FromArgb(240, 244, 248), Color.FromArgb(248, 250, 252),
            Color.FromArgb(216, 224, 234), Color.FromArgb(16, 36, 62), Color.FromArgb(95, 107, 122), Color.FromArgb(112, 123, 138),
            Color.FromArgb(11, 61, 145), Color.FromArgb(22, 136, 248), Color.FromArgb(14, 118, 221), Color.FromArgb(11, 99, 188),
            Color.FromArgb(34, 197, 94), Color.FromArgb(245, 158, 11), Color.FromArgb(239, 68, 68),
            Color.FromArgb(255, 248, 225), Color.FromArgb(215, 171, 80), Color.FromArgb(129, 77, 0),
            Color.FromArgb(235, 246, 255), Color.FromArgb(103, 174, 238), Color.FromArgb(27, 94, 32),
            Color.FromArgb(247, 250, 253), Color.FromArgb(215, 235, 255), Color.FromArgb(16, 36, 62));

        public Color AppBackground { get; private set; }
        public Color Surface { get; private set; }
        public Color SecondarySurface { get; private set; }
        public Color RaisedSurface { get; private set; }
        public Color Border { get; private set; }
        public Color Text { get; private set; }
        public Color SecondaryText { get; private set; }
        public Color DisabledText { get; private set; }
        public Color Navy { get; private set; }
        public Color Primary { get; private set; }
        public Color PrimaryHover { get; private set; }
        public Color PrimaryPressed { get; private set; }
        public Color Success { get; private set; }
        public Color Warning { get; private set; }
        public Color Error { get; private set; }
        public Color OldSurface { get; private set; }
        public Color OldBorder { get; private set; }
        public Color OldBadge { get; private set; }
        public Color CurrentSurface { get; private set; }
        public Color CurrentBorder { get; private set; }
        public Color Protected { get; private set; }
        public Color GridAlternate { get; private set; }
        public Color Selection { get; private set; }
        public Color SelectionText { get; private set; }

        private ThemePalette(
            Color appBackground, Color surface, Color secondarySurface, Color raisedSurface,
            Color border, Color text, Color secondaryText, Color disabledText,
            Color navy, Color primary, Color primaryHover, Color primaryPressed,
            Color success, Color warning, Color error,
            Color oldSurface, Color oldBorder, Color oldBadge,
            Color currentSurface, Color currentBorder, Color protectedColor,
            Color gridAlternate, Color selection, Color selectionText)
        {
            AppBackground = appBackground;
            Surface = surface;
            SecondarySurface = secondarySurface;
            RaisedSurface = raisedSurface;
            Border = border;
            Text = text;
            SecondaryText = secondaryText;
            DisabledText = disabledText;
            Navy = navy;
            Primary = primary;
            PrimaryHover = primaryHover;
            PrimaryPressed = primaryPressed;
            Success = success;
            Warning = warning;
            Error = error;
            OldSurface = oldSurface;
            OldBorder = oldBorder;
            OldBadge = oldBadge;
            CurrentSurface = currentSurface;
            CurrentBorder = currentBorder;
            Protected = protectedColor;
            GridAlternate = gridAlternate;
            Selection = selection;
            SelectionText = selectionText;
        }
    }

    internal static class ThemeManager
    {
        private const int DwmUseImmersiveDarkModeBefore20H1 = 19;
        private const int DwmUseImmersiveDarkMode = 20;
        private const int DwmBorderColor = 34;
        private const int DwmCaptionColor = 35;
        private const int DwmTextColor = 36;
        private const int DwmColorDefault = unchecked((int)0xFFFFFFFF);
        private const int DwmColorNone = unchecked((int)0xFFFFFFFE);
        private const uint RdwInvalidate = 0x0001;
        private const uint RdwFrame = 0x0400;

        internal static int TitleBarApplyCount { get; private set; }
        internal static IntPtr LastTitleBarHandle { get; private set; }
        internal static int LastImmersiveLightResult { get; private set; }
        internal static int LastBorderColorResult { get; private set; }
        internal static int LastCaptionColorResult { get; private set; }
        internal static int LastTextColorResult { get; private set; }
        internal static bool LastExplicitCaptionColorsApplied { get; private set; }

        public static ThemePalette CurrentPalette { get { return ThemePalette.Light; } }

        public static void ApplyTo(Control root)
        {
            if (root == null) return;
            var palette = ThemePalette.Light;
            var form = root as Form;
            if (form != null)
            {
                form.BackColor = palette.AppBackground;
                form.ForeColor = palette.Text;
                if (form.IsHandleCreated)
                    ApplyTitleBar(form);
                else
                {
                    form.HandleCreated -= ApplyTitleBarWhenReady;
                    form.HandleCreated += ApplyTitleBarWhenReady;
                }
            }
            ApplyControl(root, palette, palette.AppBackground);
            root.Invalidate(true);
        }

        private static void ApplyTitleBarWhenReady(object sender, EventArgs e)
        {
            var form = sender as Form;
            if (form == null) return;
            form.HandleCreated -= ApplyTitleBarWhenReady;
            ApplyTitleBar(form);
        }

        private static void ApplyControl(Control control, ThemePalette palette, Color inheritedBackColor)
        {
            var background = inheritedBackColor;
            var name = control.Name ?? string.Empty;

            if (control is Form)
                background = palette.AppBackground;
            else if (name == "HeaderLayout")
                background = palette.Surface;
            else if (name == "WorkPage" || name == "HelpPage" || control is TabPage)
                background = palette.AppBackground;
            else if (name == "OldPanel")
                background = palette.OldSurface;
            else if (name == "CurrentPanel")
                background = palette.CurrentSurface;
            else if (name == "SummaryPanel" || name == "LanguageScroll" || name == "LanguageList")
                background = palette.SecondarySurface;
            else if (control is GroupBox)
                background = palette.Surface;

            var themedTabs = control as ThemedTabControl;
            if (themedTabs != null)
            {
                themedTabs.SetPalette(palette);
                background = palette.AppBackground;
            }
            else
            {
                control.BackColor = background;
                control.ForeColor = palette.Text;
            }

            var themedInput = control as ThemedInputBorder;
            if (themedInput != null) themedInput.SetPalette(palette);
            var themedGroup = control as ThemedGroupBox;
            if (themedGroup != null) themedGroup.SetPalette(palette);

            var grid = control as DataGridView;
            if (grid != null)
            {
                ApplyGrid(grid, palette);
                background = palette.Surface;
            }
            else if (control is TextBoxBase)
            {
                control.BackColor = palette.Surface;
                control.ForeColor = palette.Text;
            }
            else if (control is Button)
            {
                ApplyButton((Button)control, palette);
            }
            else if (control is LinkLabel)
            {
                var link = (LinkLabel)control;
                link.LinkColor = palette.Primary;
                link.ActiveLinkColor = palette.PrimaryPressed;
                link.VisitedLinkColor = palette.Navy;
            }
            else if (control is Label)
            {
                ApplyLabel((Label)control, palette, background);
            }
            else if (control is RadioButton)
            {
                ((RadioButton)control).UseVisualStyleBackColor = false;
            }
            else if (control is CheckBox)
            {
                ((CheckBox)control).UseVisualStyleBackColor = false;
                var themedCheck = control as ThemedCheckBox;
                if (themedCheck != null) themedCheck.SetPalette(palette);
            }
            else if (control is ProgressBar)
            {
                control.BackColor = palette.Border;
                control.ForeColor = palette.Primary;
                var themedProgress = control as ThemedProgressBar;
                if (themedProgress != null) themedProgress.SetPalette(palette);
            }

            foreach (Control child in control.Controls)
                ApplyControl(child, palette, background);
        }

        private static void ApplyButton(Button button, ThemePalette palette)
        {
            var primary = button.Name == "AnalyzeButton" || button.Name == "CleanupButton" || button.Name == "LanguageOk";
            button.UseVisualStyleBackColor = false;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = primary ? palette.PrimaryPressed : palette.Border;
            button.BackColor = primary ? palette.Primary : palette.RaisedSurface;
            button.ForeColor = primary ? Color.White : palette.Text;
            button.FlatAppearance.MouseOverBackColor = primary ? palette.PrimaryHover : palette.SecondarySurface;
            button.FlatAppearance.MouseDownBackColor = primary ? palette.PrimaryPressed : palette.Border;
            var themedButton = button as ThemedButton;
            if (themedButton != null) themedButton.SetPalette(palette);
        }

        private static void ApplyLabel(Label label, ThemePalette palette, Color background)
        {
            label.BackColor = background;
            if (label.Name == "FolderBadge")
            {
                var old = IsInside(label, "OldPanel");
                label.BackColor = old ? palette.OldBadge : palette.Protected;
                label.ForeColor = Color.White;
            }
            else if (label.Name == "ProtectedReminder")
                label.ForeColor = palette.Protected;
            else if (label.Name == "StatusLabel" || label.Name == "FolderDescription" || label.Name == "ResultExplanation" ||
                     label.Name == "LanguageRecoveryHelp")
                label.ForeColor = palette.SecondaryText;
            else
                label.ForeColor = palette.Text;
        }

        private static bool IsInside(Control control, string ancestorName)
        {
            for (var current = control.Parent; current != null; current = current.Parent)
                if (string.Equals(current.Name, ancestorName, StringComparison.Ordinal)) return true;
            return false;
        }

        private static void ApplyGrid(DataGridView grid, ThemePalette palette)
        {
            grid.EnableHeadersVisualStyles = false;
            grid.BackgroundColor = palette.Surface;
            grid.GridColor = palette.Border;
            grid.BorderStyle = BorderStyle.FixedSingle;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            grid.ColumnHeadersDefaultCellStyle.BackColor = palette.SecondarySurface;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = palette.Text;
            grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = palette.SecondarySurface;
            grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = palette.Text;
            grid.DefaultCellStyle.BackColor = palette.Surface;
            grid.DefaultCellStyle.ForeColor = palette.Text;
            grid.DefaultCellStyle.SelectionBackColor = palette.Selection;
            grid.DefaultCellStyle.SelectionForeColor = palette.SelectionText;
            grid.AlternatingRowsDefaultCellStyle.BackColor = palette.GridAlternate;
            grid.AlternatingRowsDefaultCellStyle.ForeColor = palette.Text;
            grid.AlternatingRowsDefaultCellStyle.SelectionBackColor = palette.Selection;
            grid.AlternatingRowsDefaultCellStyle.SelectionForeColor = palette.SelectionText;
        }

        internal static void ApplyTitleBar(Form form)
        {
            if (!form.IsHandleCreated) return;
            TitleBarApplyCount++;
            LastTitleBarHandle = form.Handle;
            try
            {
                var disabled = 0;
                LastImmersiveLightResult = DwmSetWindowAttribute(form.Handle, DwmUseImmersiveDarkMode, ref disabled, sizeof(int));
                if (LastImmersiveLightResult != 0)
                    LastImmersiveLightResult = DwmSetWindowAttribute(form.Handle, DwmUseImmersiveDarkModeBefore20H1, ref disabled, sizeof(int));

                var border = DwmColorNone;
                var caption = ToColorRef(Color.White);
                var text = ToColorRef(ThemePalette.Light.Navy);
                LastBorderColorResult = DwmSetWindowAttribute(form.Handle, DwmBorderColor, ref border, sizeof(int));
                LastCaptionColorResult = DwmSetWindowAttribute(form.Handle, DwmCaptionColor, ref caption, sizeof(int));
                LastTextColorResult = DwmSetWindowAttribute(form.Handle, DwmTextColor, ref text, sizeof(int));
                LastExplicitCaptionColorsApplied = LastBorderColorResult == 0 && LastCaptionColorResult == 0 && LastTextColorResult == 0;
                if (!LastExplicitCaptionColorsApplied)
                {
                    var systemDefault = DwmColorDefault;
                    DwmSetWindowAttribute(form.Handle, DwmBorderColor, ref systemDefault, sizeof(int));
                    DwmSetWindowAttribute(form.Handle, DwmCaptionColor, ref systemDefault, sizeof(int));
                    DwmSetWindowAttribute(form.Handle, DwmTextColor, ref systemDefault, sizeof(int));
                }

                RedrawWindow(form.Handle, IntPtr.Zero, IntPtr.Zero, RdwInvalidate | RdwFrame);
            }
            catch { }
        }

        private static int ToColorRef(Color color)
        {
            return color.R | (color.G << 8) | (color.B << 16);
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int valueSize);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool RedrawWindow(IntPtr hwnd, IntPtr updateRectangle, IntPtr updateRegion, uint flags);
    }
}
