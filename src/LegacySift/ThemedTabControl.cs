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
            Padding = new Point(8, 4);
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
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
            DrawTab(e.Graphics, e.Index);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(_palette.AppBackground);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(_palette.AppBackground);
            var pageBounds = DisplayRectangle;
            using (var pageBackground = new SolidBrush(_palette.AppBackground))
                e.Graphics.FillRectangle(pageBackground, pageBounds);
            if (!_palette.IsDark && pageBounds.Width > 0 && pageBounds.Height > 0)
            {
                using (var border = new Pen(_palette.Border))
                    e.Graphics.DrawRectangle(border, pageBounds.X, pageBounds.Y,
                        Math.Max(0, pageBounds.Width - 1), Math.Max(0, pageBounds.Height - 1));
            }

            for (var index = 0; index < TabPages.Count; index++)
                DrawTab(e.Graphics, index);
        }

        private void DrawTab(Graphics graphics, int index)
        {
            if (index < 0 || index >= TabPages.Count) return;

            var bounds = GetTabRect(index);
            var selected = SelectedIndex == index;
            var selectedBackground = _palette.IsDark ? _palette.RaisedSurface : _palette.Surface;
            var idleBackground = _palette.IsDark ? _palette.AppBackground : _palette.SecondarySurface;
            using (var background = new SolidBrush(selected ? selectedBackground : idleBackground))
                graphics.FillRectangle(background, bounds);

            if (!_palette.IsDark)
            {
                using (var border = new Pen(_palette.Border))
                    graphics.DrawRectangle(border, bounds.X, bounds.Y, bounds.Width - 1, bounds.Height - 1);
            }

            if (selected)
            {
                using (var accent = new SolidBrush(_palette.Primary))
                    graphics.FillRectangle(accent, bounds.X + 1, bounds.Bottom - 3, bounds.Width - 2, 3);
            }

            // The native tab rectangle already includes translated-text
            // padding. Keep only a one-pixel border inset so the owner draw
            // does not subtract that space a second time.
            var textBounds = Rectangle.Inflate(bounds, -1, -3);
            TextRenderer.DrawText(
                graphics,
                TabPages[index].Text,
                Font,
                textBounds,
                selected ? _palette.Text : _palette.SecondaryText,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix | TextFormatFlags.NoPadding);

            if (Focused && selected)
                ControlPaint.DrawFocusRectangle(graphics, Rectangle.Inflate(bounds, -5, -5), _palette.Text, selectedBackground);
        }
    }
}
