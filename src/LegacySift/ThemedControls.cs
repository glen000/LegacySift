using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace LegacySift
{
    internal sealed class ThemedButton : Button
    {
        private ThemePalette _palette = ThemePalette.Light;

        internal void SetPalette(ThemePalette palette)
        {
            _palette = palette ?? ThemePalette.Light;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (Enabled)
            {
                base.OnPaint(e);
                return;
            }

            using (var background = new SolidBrush(_palette.SecondarySurface))
                e.Graphics.FillRectangle(background, ClientRectangle);
            using (var border = new Pen(_palette.Border))
                e.Graphics.DrawRectangle(border, 0, 0, Math.Max(0, Width - 1), Math.Max(0, Height - 1));

            TextRenderer.DrawText(
                e.Graphics,
                Text,
                Font,
                Rectangle.Inflate(ClientRectangle, -5, -2),
                _palette.DisabledText,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }
    }

    internal sealed class ThemedCheckBox : CheckBox
    {
        private ThemePalette _palette = ThemePalette.Light;

        internal void SetPalette(ThemePalette palette)
        {
            _palette = palette ?? ThemePalette.Light;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            if (Enabled)
            {
                base.OnPaint(e);
                return;
            }

            e.Graphics.Clear(BackColor);
            var state = Checked ? CheckBoxState.CheckedDisabled : CheckBoxState.UncheckedDisabled;
            var glyphSize = CheckBoxRenderer.GetGlyphSize(e.Graphics, state);
            var glyphPoint = new Point(0, Math.Max(0, (ClientSize.Height - glyphSize.Height) / 2));
            CheckBoxRenderer.DrawCheckBox(e.Graphics, glyphPoint, state);

            var textBounds = new Rectangle(
                glyphSize.Width + 5,
                0,
                Math.Max(0, ClientSize.Width - glyphSize.Width - 5),
                ClientSize.Height);
            TextRenderer.DrawText(
                e.Graphics,
                Text,
                Font,
                textBounds,
                _palette.DisabledText,
                TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);
        }
    }

    internal sealed class ThemedProgressBar : ProgressBar
    {
        private const int PbmSetBarColor = 0x0409;
        private const int PbmSetBackgroundColor = 0x2001;
        private ThemePalette _palette = ThemePalette.Light;

        internal void SetPalette(ThemePalette palette)
        {
            _palette = palette ?? ThemePalette.Light;
            if (IsHandleCreated) ApplyNativeColors();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ApplyNativeColors();
        }

        private void ApplyNativeColors()
        {
            try
            {
                // The visual-style renderer ignores ProgressBar ForeColor and
                // BackColor. Classic native painting honors PBM colors while
                // retaining the standard value and marquee semantics.
                SetWindowTheme(Handle, string.Empty, string.Empty);
                SendMessage(Handle, PbmSetBarColor, IntPtr.Zero, (IntPtr)ToColorRef(_palette.Primary));
                SendMessage(Handle, PbmSetBackgroundColor, IntPtr.Zero, (IntPtr)ToColorRef(_palette.Border));
            }
            catch { }
        }

        private static int ToColorRef(Color color)
        {
            return color.R | (color.G << 8) | (color.B << 16);
        }

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr handle, string subAppName, string subIdList);

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr handle, int message, IntPtr wParam, IntPtr lParam);
    }
}
