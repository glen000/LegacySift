using System;
using System.Drawing;
using System.Windows.Forms;

namespace LegacySift
{
    internal sealed class LanguageDialog : Form
    {
        private readonly RadioButton _italian;
        private readonly RadioButton _english;

        public AppLanguage SelectedLanguage => _italian.Checked ? AppLanguage.Italian : AppLanguage.English;

        public LanguageDialog(AppLanguage current)
        {
            Text = "Lingua / Language";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(340, 170);
            Font = new Font("Segoe UI", 9F);

            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16),
                ColumnCount = 1,
                RowCount = 4
            };
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            Controls.Add(layout);

            layout.Controls.Add(new Label
            {
                AutoSize = true,
                Font = new Font(Font, FontStyle.Bold),
                Text = "Scegli la lingua / Choose language"
            }, 0, 0);

            _italian = new RadioButton { AutoSize = true, Text = "Italiano", Checked = current == AppLanguage.Italian, Margin = new Padding(3, 12, 3, 3) };
            _english = new RadioButton { AutoSize = true, Text = "English", Checked = current == AppLanguage.English, Margin = new Padding(3, 6, 3, 3) };
            layout.Controls.Add(_italian, 0, 1);
            layout.Controls.Add(_english, 0, 2);

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, AutoSize = true };
            var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, AutoSize = true };
            var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };
            buttons.Controls.Add(ok);
            buttons.Controls.Add(cancel);
            layout.Controls.Add(buttons, 0, 3);

            AcceptButton = ok;
            CancelButton = cancel;
        }
    }
}
