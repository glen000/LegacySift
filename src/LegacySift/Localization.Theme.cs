using System.Collections.Generic;

namespace LegacySift
{
    internal static partial class L10n
    {
        private static readonly Dictionary<AppLanguage, string[]> ThemeTerms = new Dictionary<AppLanguage, string[]>
        {
            { AppLanguage.English, new[] { "Theme", "System", "Light", "Dark" } },
            { AppLanguage.Italian, new[] { "Tema", "Sistema", "Chiaro", "Scuro" } },
            { AppLanguage.German, new[] { "Design", "System", "Hell", "Dunkel" } },
            { AppLanguage.French, new[] { "Thème", "Système", "Clair", "Sombre" } },
            { AppLanguage.Spanish, new[] { "Tema", "Sistema", "Claro", "Oscuro" } },
            { AppLanguage.Portuguese, new[] { "Tema", "Sistema", "Claro", "Escuro" } },
            { AppLanguage.Polish, new[] { "Motyw", "Systemowy", "Jasny", "Ciemny" } },
            { AppLanguage.Dutch, new[] { "Thema", "Systeem", "Licht", "Donker" } },
            { AppLanguage.Turkish, new[] { "Tema", "Sistem", "Açık", "Koyu" } },
            { AppLanguage.Ukrainian, new[] { "Тема", "Системна", "Світла", "Темна" } },
            { AppLanguage.ChineseSimplified, new[] { "主题", "跟随系统", "浅色", "深色" } },
            { AppLanguage.Japanese, new[] { "テーマ", "システム", "ライト", "ダーク" } },
            { AppLanguage.Hindi, new[] { "थीम", "सिस्टम", "हल्का", "गहरा" } },
            { AppLanguage.Romanian, new[] { "Temă", "Sistem", "Luminoasă", "Întunecată" } },
            { AppLanguage.Czech, new[] { "Motiv", "Systémový", "Světlý", "Tmavý" } },
            { AppLanguage.Greek, new[] { "Θέμα", "Σύστημα", "Φωτεινό", "Σκοτεινό" } },
            { AppLanguage.Hungarian, new[] { "Téma", "Rendszer", "Világos", "Sötét" } },
            { AppLanguage.Swedish, new[] { "Tema", "System", "Ljust", "Mörkt" } },
            { AppLanguage.Korean, new[] { "테마", "시스템", "라이트", "다크" } },
            { AppLanguage.Indonesian, new[] { "Tema", "Sistem", "Terang", "Gelap" } },
            { AppLanguage.Vietnamese, new[] { "Giao diện", "Hệ thống", "Sáng", "Tối" } },
            { AppLanguage.Danish, new[] { "Tema", "System", "Lys", "Mørk" } },
            { AppLanguage.NorwegianBokmal, new[] { "Tema", "System", "Lys", "Mørk" } },
            { AppLanguage.Finnish, new[] { "Teema", "Järjestelmä", "Vaalea", "Tumma" } },
            { AppLanguage.Slovak, new[] { "Motív", "Systém", "Svetlý", "Tmavý" } },
            { AppLanguage.Bulgarian, new[] { "Тема", "Системна", "Светла", "Тъмна" } },
            { AppLanguage.Croatian, new[] { "Tema", "Sustav", "Svijetla", "Tamna" } },
            { AppLanguage.Bengali, new[] { "থিম", "সিস্টেম", "হালকা", "গাঢ়" } },
            { AppLanguage.Thai, new[] { "ธีม", "ระบบ", "สว่าง", "มืด" } },
            { AppLanguage.Malay, new[] { "Tema", "Sistem", "Cerah", "Gelap" } },
            { AppLanguage.Filipino, new[] { "Tema", "System", "Maliwanag", "Madilim" } },
            { AppLanguage.Estonian, new[] { "Teema", "Süsteem", "Hele", "Tume" } },
            { AppLanguage.Latvian, new[] { "Motīvs", "Sistēma", "Gaišs", "Tumšs" } },
            { AppLanguage.Lithuanian, new[] { "Tema", "Sistema", "Šviesi", "Tamsi" } }
        };

        private static Dictionary<string, string> WithThemeStrings(AppLanguage language, Dictionary<string, string> dictionary)
        {
            string[] terms;
            if (!ThemeTerms.TryGetValue(language, out terms)) terms = ThemeTerms[AppLanguage.English];
            dictionary["ThemeButton"] = terms[0] + "…";
            dictionary["ThemeTitle"] = terms[0];
            dictionary["ThemeSystem"] = terms[1];
            dictionary["ThemeLight"] = terms[2];
            dictionary["ThemeDark"] = terms[3];
            return dictionary;
        }
    }
}
