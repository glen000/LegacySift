using System;
using System.Drawing;
using System.Windows.Forms;

namespace LegacySift
{
    internal sealed class ThemedTabControl : TabControl
    {
        private const int WmPaint = 0x000F;
        private const int WmPrint = 0x0317;
        private const int WmPrintClient = 0x0318;
        private ThemePalette _palette = ThemePalette.Light;

        public ThemedTabControl()
        {
            DrawMode = TabDrawMode.OwnerDrawFixed;
            // Keep translated result tabs on one compact desktop row. Text is
            // never shortened; only decorative horizontal padding is reduced.
            Padding = new Point(8, 4);
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
            DrawTab(e.Graphics, e.Index);
        }


        protected override void WndProc(ref Message message)
        {
            var paint = message.Msg == WmPaint;
            var print = message.Msg == WmPrint || message.Msg == WmPrintClient;
            base.WndProc(ref message);
            if (!paint && !print) return;

            try
            {
                using (var graphics = print && message.WParam != IntPtr.Zero
                    ? Graphics.FromHdc(message.WParam)
                    : Graphics.FromHwnd(Handle))
                    PaintPaneOverlay(graphics);
            }
            catch { }
        }

        private void PaintPaneOverlay(Graphics graphics)
        {
            var page = DisplayRectangle;
            if (page.Width <= 0 || page.Height <= 0) return;

            var headerBottom = page.Top;
            for (var index = 0; index < TabPages.Count; index++)
                headerBottom = Math.Max(headerBottom, GetTabRect(index).Bottom);

            using (var background = new SolidBrush(_palette.AppBackground))
            {
                // The native control paints the unused header strip and the
                // gaps around owner-drawn tabs with the Windows light theme.
                // Cover the complete strip first, then redraw real tabs below.
                graphics.FillRectangle(background, 0, 0, ClientSize.Width,
                    Math.Min(ClientSize.Height, headerBottom + 1));
                if (page.Left > 0)
                    graphics.FillRectangle(background, 0, 0, page.Left, ClientSize.Height);
                if (page.Right < ClientSize.Width)
                    graphics.FillRectangle(background, page.Right, 0, ClientSize.Width - page.Right, ClientSize.Height);
                if (page.Bottom < ClientSize.Height)
                    graphics.FillRectangle(background, 0, page.Bottom, ClientSize.Width, ClientSize.Height - page.Bottom);
            }

            if (!_palette.IsDark)
            {
                using (var border = new Pen(_palette.Border))
                    graphics.DrawRectangle(border, page.X - 1, page.Y - 1, page.Width + 1, page.Height + 1);
            }

            // Redraw headers last so the selected underline and focus cue sit
            // above the pane-edge cover while native rectangles remain intact.
            for (var index = 0; index < TabPages.Count; index++)
                DrawTab(graphics, index);
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
