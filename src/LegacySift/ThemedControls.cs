using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace LegacySift
{
    internal sealed class ThemedInputBorder : Panel
    {
        private ThemePalette _palette = ThemePalette.Light;

        public ThemedInputBorder()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            Padding = new Padding(1);
        }

        internal void SetPalette(ThemePalette palette)
        {
            _palette = palette ?? ThemePalette.Light;
            BackColor = _palette.Border;
            Invalidate();
        }

        protected override void OnControlAdded(ControlEventArgs e)
        {
            base.OnControlAdded(e);
            e.Control.GotFocus += ChildFocusChanged;
            e.Control.LostFocus += ChildFocusChanged;
        }

        protected override void OnControlRemoved(ControlEventArgs e)
        {
            e.Control.GotFocus -= ChildFocusChanged;
            e.Control.LostFocus -= ChildFocusChanged;
            base.OnControlRemoved(e);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (var pen = new Pen(ContainsFocus ? _palette.Primary : _palette.Border))
                e.Graphics.DrawRectangle(pen, 0, 0, Math.Max(0, Width - 1), Math.Max(0, Height - 1));
        }

        private void ChildFocusChanged(object sender, EventArgs e)
        {
            Invalidate();
        }
    }

    internal sealed class ThemedGroupBox : GroupBox
    {
        private ThemePalette _palette = ThemePalette.Light;

        public ThemedGroupBox()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        }

        internal void SetPalette(ThemePalette palette)
        {
            _palette = palette ?? ThemePalette.Light;
            BackColor = _palette.Surface;
            ForeColor = _palette.Text;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(BackColor);
            var title = TextRenderer.MeasureText(e.Graphics, Text, Font, new Size(int.MaxValue, int.MaxValue), TextFormatFlags.NoPadding);
            var titleBounds = new Rectangle(8, 0, Math.Min(title.Width + 4, Math.Max(0, Width - 16)), Math.Max(Font.Height + 2, title.Height));
            TextRenderer.DrawText(e.Graphics, Text, Font, titleBounds, ForeColor,
                TextFormatFlags.Left | TextFormatFlags.Top | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);
            var lineY = Math.Max(1, titleBounds.Height / 2);
            var lineStart = Math.Min(Width - 1, titleBounds.Right + 6);
            if (lineStart < Width - 1)
            {
                using (var pen = new Pen(_palette.Border))
                    e.Graphics.DrawLine(pen, lineStart, lineY, Width - 1, lineY);
            }
        }
    }

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
