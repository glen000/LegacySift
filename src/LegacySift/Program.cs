using System;
using System.Windows.Forms;

namespace LegacySift
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            L10n.SetLanguage(SettingsStore.LoadLanguage());
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }
}
