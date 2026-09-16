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
            Padding = new Point(12, 4);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
        }

        public void SetPalette(ThemePalette palette)
        {
            _palette = palette ?? ThemePalette.Light;
            BackColor = _palette.AppBackground;
            ForeColor = _palette.Text;
            Invalidate();
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

            var textBounds = Rectangle.Inflate(bounds, -8, -3);
            TextRenderer.DrawText(
                e.Graphics,
                TabPages[e.Index].Text,
                Font,
                textBounds,
                selected ? _palette.Text : _palette.SecondaryText,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPrefix);

            if (Focused && selected)
                ControlPaint.DrawFocusRectangle(e.Graphics, Rectangle.Inflate(bounds, -5, -5), _palette.Text, _palette.Surface);
        }
    }
}
