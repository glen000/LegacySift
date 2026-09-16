using System.Drawing;
using System.Windows.Forms;

namespace LegacySift
{
    internal sealed class ThemeDialog : Form
    {
        private readonly RadioButton _system;
        private readonly RadioButton _light;
        private readonly RadioButton _dark;

        public ThemeMode SelectedMode
        {
            get
            {
                if (_dark.Checked) return ThemeMode.Dark;
                if (_light.Checked) return ThemeMode.Light;
                return ThemeMode.System;
            }
        }

        public ThemeDialog(ThemeMode current)
        {
            Name = "ThemeDialog";
            Text = L10n.T("ThemeTitle");
            Icon = AppIcon.CreateIcon();
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(390, 250);
            Font = new Font("Segoe UI", 9F);
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;

            var root = new TableLayoutPanel
            {
                Name = "ThemeRoot",
                Dock = DockStyle.Fill,
                Padding = new Padding(18),
                ColumnCount = 1,
                RowCount = 5
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            Controls.Add(root);

            root.Controls.Add(new Label
            {
                Name = "ThemeHeading",
                Text = L10n.T("ThemeTitle"),
                AutoSize = true,
                Font = new Font(Font.FontFamily, 12F, FontStyle.Bold),
                Margin = new Padding(0, 0, 0, 8)
            }, 0, 0);

            _system = CreateChoice("ThemeSystem", L10n.T("ThemeSystem"), current == ThemeMode.System);
            _light = CreateChoice("ThemeLight", L10n.T("ThemeLight"), current == ThemeMode.Light);
            _dark = CreateChoice("ThemeDark", L10n.T("ThemeDark"), current == ThemeMode.Dark);
            root.Controls.Add(_system, 0, 1);
            root.Controls.Add(_light, 0, 2);
            root.Controls.Add(_dark, 0, 3);

            var buttons = new FlowLayoutPanel
            {
                Name = "ThemeButtons",
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                Margin = new Padding(0, 14, 0, 0)
            };
            var ok = new Button { Name = "ThemeOk", Text = "OK", DialogResult = DialogResult.OK, AutoSize = true, MinimumSize = new Size(90, 32) };
            var cancel = new Button { Name = "ThemeCancel", Text = L10n.T("Cancel"), DialogResult = DialogResult.Cancel, AutoSize = true, MinimumSize = new Size(100, 32) };
            buttons.Controls.Add(ok);
            buttons.Controls.Add(cancel);
            root.Controls.Add(buttons, 0, 4);

            AcceptButton = ok;
            CancelButton = cancel;
            ThemeManager.ApplyTo(this);
        }

        private static RadioButton CreateChoice(string name, string text, bool isChecked)
        {
            return new RadioButton
            {
                Name = name,
                Text = text,
                Checked = isChecked,
                AutoSize = false,
                Dock = DockStyle.Top,
                Height = 38,
                Padding = new Padding(8, 0, 8, 0),
                Margin = new Padding(0, 1, 0, 1)
            };
        }
    }
}
