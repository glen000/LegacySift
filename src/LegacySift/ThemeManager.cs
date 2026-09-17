using Microsoft.Win32;
using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace LegacySift
{
    internal enum ThemeMode
    {
        System,
        Light,
        Dark
    }

    internal sealed class ThemePalette
    {
        public static readonly ThemePalette Light = new ThemePalette(
            false,
            Color.FromArgb(245, 247, 250), Color.White, Color.FromArgb(240, 244, 248), Color.FromArgb(248, 250, 252),
            Color.FromArgb(216, 224, 234), Color.FromArgb(16, 36, 62), Color.FromArgb(95, 107, 122), Color.FromArgb(112, 123, 138),
            Color.FromArgb(11, 61, 145), Color.FromArgb(22, 136, 248), Color.FromArgb(14, 118, 221), Color.FromArgb(11, 99, 188),
            Color.FromArgb(34, 197, 94), Color.FromArgb(245, 158, 11), Color.FromArgb(239, 68, 68),
            Color.FromArgb(255, 248, 225), Color.FromArgb(215, 171, 80), Color.FromArgb(129, 77, 0),
            Color.FromArgb(235, 246, 255), Color.FromArgb(103, 174, 238), Color.FromArgb(27, 94, 32),
            Color.FromArgb(247, 250, 253), Color.FromArgb(215, 235, 255), Color.FromArgb(16, 36, 62));

        public static readonly ThemePalette Dark = new ThemePalette(
            true,
            Color.FromArgb(15, 23, 32), Color.FromArgb(21, 31, 43), Color.FromArgb(26, 39, 53), Color.FromArgb(33, 49, 66),
            Color.FromArgb(48, 65, 84), Color.FromArgb(231, 237, 244), Color.FromArgb(170, 183, 197), Color.FromArgb(113, 128, 150),
            Color.FromArgb(69, 163, 255), Color.FromArgb(37, 137, 245), Color.FromArgb(59, 154, 247), Color.FromArgb(20, 116, 212),
            Color.FromArgb(69, 201, 122), Color.FromArgb(217, 164, 65), Color.FromArgb(237, 106, 106),
            Color.FromArgb(35, 38, 42), Color.FromArgb(93, 78, 52), Color.FromArgb(169, 117, 29),
            Color.FromArgb(24, 38, 49), Color.FromArgb(46, 86, 105), Color.FromArgb(69, 201, 122),
            Color.FromArgb(24, 36, 50), Color.FromArgb(22, 78, 122), Color.FromArgb(231, 237, 244));

        public bool IsDark { get; private set; }
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
            bool isDark,
            Color appBackground, Color surface, Color secondarySurface, Color raisedSurface,
            Color border, Color text, Color secondaryText, Color disabledText,
            Color navy, Color primary, Color primaryHover, Color primaryPressed,
            Color success, Color warning, Color error,
            Color oldSurface, Color oldBorder, Color oldBadge,
            Color currentSurface, Color currentBorder, Color protectedColor,
            Color gridAlternate, Color selection, Color selectionText)
        {
            IsDark = isDark;
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
        private const uint SwpNoSize = 0x0001;
        private const uint SwpNoMove = 0x0002;
        private const uint SwpNoZOrder = 0x0004;
        private const uint SwpNoActivate = 0x0010;
        private const uint SwpFrameChanged = 0x0020;

        internal static int TitleBarApplyCount { get; private set; }
        internal static bool LastRequestedTitleBarDark { get; private set; }
        internal static IntPtr LastTitleBarHandle { get; private set; }
        internal static int LastImmersiveDarkResult { get; private set; }
        internal static int LastBorderColorResult { get; private set; }
        internal static int LastCaptionColorResult { get; private set; }
        internal static int LastTextColorResult { get; private set; }
        internal static bool LastExplicitCaptionColorsApplied { get; private set; }

        public static ThemeMode CurrentMode { get; private set; } = ThemeMode.System;
        public static ThemePalette CurrentPalette { get; private set; } = ResolvePalette(ThemeMode.System);

        public static void SetMode(ThemeMode mode)
        {
            CurrentMode = mode;
            CurrentPalette = ResolvePalette(mode);
        }

        public static ThemePalette ResolvePalette(ThemeMode mode)
        {
            if (mode == ThemeMode.Dark) return ThemePalette.Dark;
            if (mode == ThemeMode.Light) return ThemePalette.Light;
            return IsWindowsAppThemeDark() ? ThemePalette.Dark : ThemePalette.Light;
        }

        internal static bool IsWindowsAppThemeDark()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    var value = key == null ? null : key.GetValue("AppsUseLightTheme");
                    if (value is int) return (int)value == 0;
                }
            }
            catch { }
            return false;
        }

        public static void ApplyTo(Control root)
        {
            if (root == null) return;
            var palette = CurrentPalette;
            var form = root as Form;
            if (form != null)
            {
                form.BackColor = palette.AppBackground;
                form.ForeColor = palette.Text;
                if (form.IsHandleCreated)
                    ApplyTitleBar(form, palette.IsDark);
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
            ApplyTitleBar(form, CurrentPalette.IsDark);
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
                link.LinkColor = palette.IsDark ? palette.Navy : palette.Primary;
                link.ActiveLinkColor = palette.PrimaryPressed;
                link.VisitedLinkColor = palette.IsDark ? palette.Primary : palette.Navy;
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
            var primary = button.Name == "AnalyzeButton" || button.Name == "CleanupButton" ||
                          button.Name == "ThemeOk" || button.Name == "LanguageOk";
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
                     label.Name == "LanguageRecoveryHelp" || label.Name == "ThemeHint")
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
            grid.BorderStyle = palette.IsDark ? BorderStyle.None : BorderStyle.FixedSingle;
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.ColumnHeadersBorderStyle = palette.IsDark ? DataGridViewHeaderBorderStyle.None : DataGridViewHeaderBorderStyle.Single;
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

        internal static void ApplyTitleBar(Form form, bool dark)
        {
            if (!form.IsHandleCreated) return;
            TitleBarApplyCount++;
            LastRequestedTitleBarDark = dark;
            LastTitleBarHandle = form.Handle;
            try
            {
                var enabled = dark ? 1 : 0;
                LastImmersiveDarkResult = DwmSetWindowAttribute(form.Handle, DwmUseImmersiveDarkMode, ref enabled, sizeof(int));
                if (LastImmersiveDarkResult != 0)
                    LastImmersiveDarkResult = DwmSetWindowAttribute(form.Handle, DwmUseImmersiveDarkModeBefore20H1, ref enabled, sizeof(int));

                // Windows 11 supports explicit non-client colors. These calls
                // fail harmlessly with E_INVALIDARG on older Windows builds,
                // where the immersive-dark hint above remains the fallback.
                var palette = dark ? ThemePalette.Dark : ThemePalette.Light;
                var border = ToColorRef(palette.Border);
                var caption = ToColorRef(dark ? palette.Surface : Color.White);
                var text = ToColorRef(dark ? palette.Text : palette.Navy);
                LastBorderColorResult = DwmSetWindowAttribute(form.Handle, DwmBorderColor, ref border, sizeof(int));
                LastCaptionColorResult = DwmSetWindowAttribute(form.Handle, DwmCaptionColor, ref caption, sizeof(int));
                LastTextColorResult = DwmSetWindowAttribute(form.Handle, DwmTextColor, ref text, sizeof(int));
                LastExplicitCaptionColorsApplied = LastBorderColorResult == 0 && LastCaptionColorResult == 0 && LastTextColorResult == 0;
                if (!LastExplicitCaptionColorsApplied)
                {
                    // Do not leave a partially supported combination behind.
                    // Supported attributes are restored to OS defaults while
                    // unsupported attributes continue to fail harmlessly.
                    var systemDefault = unchecked((int)0xFFFFFFFF);
                    DwmSetWindowAttribute(form.Handle, DwmBorderColor, ref systemDefault, sizeof(int));
                    DwmSetWindowAttribute(form.Handle, DwmCaptionColor, ref systemDefault, sizeof(int));
                    DwmSetWindowAttribute(form.Handle, DwmTextColor, ref systemDefault, sizeof(int));
                }

                // Refresh the native frame without recreating the form handle,
                // preserving window state, taskbar identity and client layout.
                SetWindowPos(form.Handle, IntPtr.Zero, 0, 0, 0, 0,
                    SwpNoMove | SwpNoSize | SwpNoZOrder | SwpNoActivate | SwpFrameChanged);
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
        private static extern bool SetWindowPos(IntPtr hwnd, IntPtr insertAfter, int x, int y, int cx, int cy, uint flags);
    }
}
