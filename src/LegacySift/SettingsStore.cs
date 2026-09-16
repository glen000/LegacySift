using System;
using System.IO;

namespace LegacySift
{
    internal static class SettingsStore
    {
        private static string SettingsPath
        {
            get
            {
                var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LegacySift");
                Directory.CreateDirectory(dir);
                return Path.Combine(dir, "settings.ini");
            }
        }

        public static AppLanguage LoadLanguage()
        {
            try
            {
                if (!File.Exists(SettingsPath)) return L10n.DetectDefaultLanguage();
                return ParseLanguageSetting(File.ReadAllLines(SettingsPath), L10n.DetectDefaultLanguage());
            }
            catch { }
            return L10n.DetectDefaultLanguage();
        }

        public static void SaveLanguage(AppLanguage language)
        {
            try
            {
                var lines = File.Exists(SettingsPath) ? File.ReadAllLines(SettingsPath) : null;
                var theme = ParseThemeSetting(lines, ThemeMode.System);
                File.WriteAllText(SettingsPath, SerializeSettings(language, theme));
            }
            catch { }
        }

        public static ThemeMode LoadTheme()
        {
            try
            {
                if (!File.Exists(SettingsPath)) return ThemeMode.System;
                return ParseThemeSetting(File.ReadAllLines(SettingsPath), ThemeMode.System);
            }
            catch { }
            return ThemeMode.System;
        }

        public static void SaveTheme(ThemeMode theme)
        {
            try
            {
                var lines = File.Exists(SettingsPath) ? File.ReadAllLines(SettingsPath) : null;
                var language = ParseLanguageSetting(lines, L10n.DetectDefaultLanguage());
                File.WriteAllText(SettingsPath, SerializeSettings(language, theme));
            }
            catch { }
        }

        internal static AppLanguage ParseLanguageSetting(string[] lines, AppLanguage fallback)
        {
            if (lines == null) return fallback;
            foreach (var line in lines)
            {
                if (line == null || !line.StartsWith("language=", StringComparison.OrdinalIgnoreCase)) continue;
                return L10n.FromCode(line.Substring("language=".Length));
            }
            return fallback;
        }

        internal static string SerializeLanguageSetting(AppLanguage language)
        {
            return "language=" + L10n.ToCode(language) + "\r\n";
        }

        internal static ThemeMode ParseThemeSetting(string[] lines, ThemeMode fallback)
        {
            if (lines == null) return fallback;
            foreach (var line in lines)
            {
                if (line == null || !line.StartsWith("theme=", StringComparison.OrdinalIgnoreCase)) continue;
                switch (line.Substring("theme=".Length).Trim().ToLowerInvariant())
                {
                    case "light": return ThemeMode.Light;
                    case "dark": return ThemeMode.Dark;
                    case "system": return ThemeMode.System;
                    default: return fallback;
                }
            }
            return fallback;
        }

        internal static string SerializeSettings(AppLanguage language, ThemeMode theme)
        {
            return "language=" + L10n.ToCode(language) + "\r\n" +
                   "theme=" + theme.ToString().ToLowerInvariant() + "\r\n";
        }
    }
}
