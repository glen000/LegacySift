using System;
using System.Drawing;
using System.Windows.Forms;

namespace LegacySift
{
    internal sealed class ThemedTabControl : TabControl
    {
        private ThemePalette _palette = ThemePalette.Light;

        public ThemedTabControl()
        {
            DrawMode = TabDrawMode.OwnerDrawFixed;
            // Keep translated result tabs on one compact desktop row. Text is
            // never shortened; only decorative horizontal padding is reduced.
            Padding = new Point(6, 4);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
        }

        public void SetPalette(ThemePalette palette)
        {
            _palette = palette ?? ThemePalette.Light;
            BackColor = _palette.AppBackground;
            ForeColor = _palette.Text;
            Invalidate();
        }

        protected override void OnFontChanged(EventArgs e)
        {
            base.OnFontChanged(e);
            // Owner-drawn native tabs cache their header widths. Recreate the
            // handle after a DPI/font change so translated labels are measured
            // again instead of being drawn into their previous 96-DPI bounds.
            if (IsHandleCreated && !Disposing && !IsDisposed)
                RecreateHandle();
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= TabPages.Count) return;

            var bounds = GetTabRect(e.Index);
            var selected = SelectedIndex == e.Index;
            using (var background = new SolidBrush(selected ? _palette.Surface : _palette.SecondarySurface))
                e.Graphics.FillRectangle(background, bounds);

            using (var border = new Pen(_palette.Border))
                e.Graphics.DrawRectangle(border, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);

            if (selected)
            {
                using (var accent = new SolidBrush(_palette.Primary))
                    e.Graphics.FillRectangle(accent, bounds.X + 1, bounds.Y + 1, bounds.Width - 2, 3);
            }

            // The native tab rectangle already includes translated-text
            // padding. Keep only a one-pixel border inset so the owner draw
            // does not subtract that space a second time.
            var textBounds = Rectangle.Inflate(bounds, -1, -3);
            TextRenderer.DrawText(
                e.Graphics,
                TabPages[e.Index].Text,
                Font,
                textBounds,
                selected ? _palette.Text : _palette.SecondaryText,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);

            if (Focused && selected)
                ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(bounds, -5, -5), _palette.Text, _palette.Surface);
        }
    }
}
