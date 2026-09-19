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
            Dictionary<string, string> dictionary;
            switch (language)
            {
                case AppLanguage.Italian: dictionary = It; break;
                case AppLanguage.German: dictionary = De; break;
                case AppLanguage.French: dictionary = Fr; break;
                case AppLanguage.Spanish: dictionary = Es; break;
                case AppLanguage.Portuguese: dictionary = Pt; break;
                case AppLanguage.Polish: dictionary = Pl; break;
                case AppLanguage.Dutch: dictionary = Nl; break;
                case AppLanguage.Turkish: dictionary = Tr; break;
                case AppLanguage.Ukrainian: dictionary = Uk; break;
                case AppLanguage.ChineseSimplified: dictionary = ZhHans; break;
                case AppLanguage.Japanese: dictionary = Ja; break;
                case AppLanguage.Hindi: dictionary = Hi; break;
                case AppLanguage.Romanian: dictionary = Ro; break;
                case AppLanguage.Czech: dictionary = Cs; break;
                case AppLanguage.Greek: dictionary = El; break;
                case AppLanguage.Hungarian: dictionary = Hu; break;
                case AppLanguage.Swedish: dictionary = Sv; break;
                case AppLanguage.Korean: dictionary = Ko; break;
                case AppLanguage.Indonesian: dictionary = Id; break;
                case AppLanguage.Vietnamese: dictionary = Vi; break;
                case AppLanguage.Danish: dictionary = Da; break;
                case AppLanguage.NorwegianBokmal: dictionary = Nb; break;
                case AppLanguage.Finnish: dictionary = Fi; break;
                case AppLanguage.Slovak: dictionary = Sk; break;
                case AppLanguage.Bulgarian: dictionary = Bg; break;
                case AppLanguage.Croatian: dictionary = Hr; break;
                case AppLanguage.Bengali: dictionary = Bn; break;
                case AppLanguage.Thai: dictionary = Th; break;
                case AppLanguage.Malay: dictionary = Ms; break;
                case AppLanguage.Filipino: dictionary = Fil; break;
                case AppLanguage.Estonian: dictionary = Et; break;
                case AppLanguage.Latvian: dictionary = Lv; break;
                case AppLanguage.Lithuanian: dictionary = Lt; break;
                default: dictionary = En; break;
            }
            return dictionary;
        }
    }
}
