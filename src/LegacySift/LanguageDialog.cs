using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace LegacySift
{
    internal sealed class LanguageDialog : Form
    {
        private readonly List<RadioButton> _choices = new List<RadioButton>();
        private readonly List<Image> _flagImages = new List<Image>();

        public AppLanguage SelectedLanguage
        {
            get
            {
                foreach (var choice in _choices)
                {
                    if (choice.Checked && choice.Tag is AppLanguage)
                        return (AppLanguage)choice.Tag;
                }
                return AppLanguage.English;
            }
        }

        public LanguageDialog(AppLanguage current)
        {
            // Intentionally not localized. If someone selects an unfamiliar language,
            // this dialog must always remain easy to find and understand.
            Text = "Language / Lingua";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(530, 575);
            Font = new Font("Segoe UI", 9F);
            AutoScaleMode = AutoScaleMode.Dpi;

            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(16),
                ColumnCount = 1,
                RowCount = 4
            };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Controls.Add(root);

            root.Controls.Add(new Label
            {
                AutoSize = true,
                Font = new Font(Font.FontFamily, 12F, FontStyle.Bold),
                Text = "Choose language / Scegli la lingua"
            }, 0, 0);

            root.Controls.Add(new Label
            {
                AutoSize = true,
                ForeColor = SystemColors.GrayText,
                Margin = new Padding(0, 5, 0, 10),
                Text = "Flags, native names and language codes always stay the same.\r\n" +
                       "Bandiere, nomi originali e codici restano sempre uguali."
            }, 0, 1);

            var scroll = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = SystemColors.Window,
                Padding = new Padding(8)
            };

            var list = new TableLayoutPanel
            {
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                ColumnCount = 1,
                RowCount = LanguageCatalog.All.Count,
                Dock = DockStyle.Top,
                BackColor = SystemColors.Window
            };

            var row = 0;
            foreach (var info in LanguageCatalog.All)
            {
                var flag = LanguageCatalog.CreateFlag(info.Language);
                _flagImages.Add(flag);

                var choice = new RadioButton
                {
                    AutoSize = false,
                    Height = 34,
                    Dock = DockStyle.Top,
                    Text = info.DisplayName,
                    Tag = info.Language,
                    Checked = info.Language == current,
                    Image = flag,
                    ImageAlign = ContentAlignment.MiddleLeft,
                    TextImageRelation = TextImageRelation.ImageBeforeText,
                    Padding = new Padding(4, 0, 4, 0),
                    Margin = new Padding(2),
                    UseVisualStyleBackColor = true
                };

                _choices.Add(choice);
                list.RowStyles.Add(new RowStyle(SizeType.Absolute, 38F));
                list.Controls.Add(choice, 0, row++);
            }

            scroll.Controls.Add(list);
            root.Controls.Add(scroll, 0, 2);

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                AutoSize = true,
                Margin = new Padding(0, 12, 0, 0)
            };
            var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, AutoSize = true, MinimumSize = new Size(90, 32) };
            var cancel = new Button { Text = "Cancel / Annulla", DialogResult = DialogResult.Cancel, AutoSize = true, MinimumSize = new Size(130, 32) };
            buttons.Controls.Add(ok);
            buttons.Controls.Add(cancel);
            root.Controls.Add(buttons, 0, 3);

            AcceptButton = ok;
            CancelButton = cancel;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                foreach (var image in _flagImages)
                    image.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
