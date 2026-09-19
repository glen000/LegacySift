using System;
using System.Collections.Generic;

namespace LegacySift
{
    internal static partial class L10n
    {
        private static Dictionary<string, string> Make(params string[] pairs)
        {
            if (pairs == null || pairs.Length % 2 != 0)
                throw new ArgumentException("Translation pairs must contain key/value pairs.");

            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            for (var i = 0; i < pairs.Length; i += 2)
                result[pairs[i]] = pairs[i + 1];
            return result;
        }

        private static Dictionary<string, string> GetDictionary(AppLanguage language)
        {
            switch (language)
            {
                case AppLanguage.Italian: return It;
                case AppLanguage.German: return De;
                case AppLanguage.French: return Fr;
                case AppLanguage.Spanish: return Es;
                case AppLanguage.Portuguese: return Pt;
                case AppLanguage.Polish: return Pl;
                case AppLanguage.Dutch: return Nl;
                case AppLanguage.Turkish: return Tr;
                case AppLanguage.Ukrainian: return Uk;
                case AppLanguage.ChineseSimplified: return ZhHans;
                case AppLanguage.Japanese: return Ja;
                case AppLanguage.Hindi: return Hi;
                case AppLanguage.Romanian: return Ro;
                case AppLanguage.Czech: return Cs;
                case AppLanguage.Greek: return El;
                case AppLanguage.Hungarian: return Hu;
                case AppLanguage.Swedish: return Sv;
                case AppLanguage.Korean: return Ko;
                case AppLanguage.Indonesian: return Id;
                case AppLanguage.Vietnamese: return Vi;
                case AppLanguage.Danish: return Da;
                case AppLanguage.NorwegianBokmal: return Nb;
                case AppLanguage.Finnish: return Fi;
                case AppLanguage.Slovak: return Sk;
                case AppLanguage.Bulgarian: return Bg;
                case AppLanguage.Croatian: return Hr;
                case AppLanguage.Bengali: return Bn;
                case AppLanguage.Thai: return Th;
                case AppLanguage.Malay: return Ms;
                case AppLanguage.Filipino: return Fil;
                case AppLanguage.Estonian: return Et;
                case AppLanguage.Latvian: return Lv;
                case AppLanguage.Lithuanian: return Lt;
                default: return En;
            }
        }
    }
}
