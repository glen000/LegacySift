using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;

namespace LegacySift
{
    internal sealed class LanguageInfo
    {
        public AppLanguage Language { get; private set; }
        public string Code { get; private set; }
        public string NativeName { get; private set; }
        public string EnglishName { get; private set; }
        public FlagKind Flag { get; private set; }

        public LanguageInfo(AppLanguage language, string code, string nativeName, string englishName, FlagKind flag)
        {
            Language = language;
            Code = code;
            NativeName = nativeName;
            EnglishName = englishName;
            Flag = flag;
        }

        public string DisplayName
        {
            get
            {
                if (string.Equals(NativeName, EnglishName, StringComparison.OrdinalIgnoreCase))
                    return NativeName + "   [" + Code + "]";
                return NativeName + "   —   " + EnglishName + "   [" + Code + "]";
            }
        }
    }

    internal enum FlagKind
    {
        UnitedKingdom,
        Italy,
        Germany,
        France,
        Spain,
        Portugal,
        Poland,
        Netherlands,
        Turkey,
        Russia,
        Ukraine,
        China,
        Japan,
        India
    }

    internal static class LanguageCatalog
    {
        private static readonly List<LanguageInfo> Items = new List<LanguageInfo>
        {
            new LanguageInfo(AppLanguage.English, "EN", "English", "English", FlagKind.UnitedKingdom),
            new LanguageInfo(AppLanguage.Italian, "IT", "Italiano", "Italian", FlagKind.Italy),
            new LanguageInfo(AppLanguage.German, "DE", "Deutsch", "German", FlagKind.Germany),
            new LanguageInfo(AppLanguage.French, "FR", "Français", "French", FlagKind.France),
            new LanguageInfo(AppLanguage.Spanish, "ES", "Español", "Spanish", FlagKind.Spain),
            new LanguageInfo(AppLanguage.Portuguese, "PT", "Português", "Portuguese", FlagKind.Portugal),
            new LanguageInfo(AppLanguage.Polish, "PL", "Polski", "Polish", FlagKind.Poland),
            new LanguageInfo(AppLanguage.Dutch, "NL", "Nederlands", "Dutch", FlagKind.Netherlands),
            new LanguageInfo(AppLanguage.Turkish, "TR", "Türkçe", "Turkish", FlagKind.Turkey),
            new LanguageInfo(AppLanguage.Russian, "RU", "Русский", "Russian", FlagKind.Russia),
            new LanguageInfo(AppLanguage.Ukrainian, "UK", "Українська", "Ukrainian", FlagKind.Ukraine),
            new LanguageInfo(AppLanguage.ChineseSimplified, "ZH", "简体中文", "Chinese (Simplified)", FlagKind.China),
            new LanguageInfo(AppLanguage.Japanese, "JA", "日本語", "Japanese", FlagKind.Japan),
            new LanguageInfo(AppLanguage.Hindi, "HI", "हिन्दी", "Hindi", FlagKind.India)
        };

        public static IList<LanguageInfo> All
        {
            get { return Items.AsReadOnly(); }
        }

        public static LanguageInfo Get(AppLanguage language)
        {
            return Items.FirstOrDefault(x => x.Language == language) ?? Items[0];
        }

        public static Bitmap CreateFlag(AppLanguage language)
        {
            return CreateFlag(Get(language).Flag, 28, 18);
        }

        private static Bitmap CreateFlag(FlagKind kind, int width, int height)
        {
            var bitmap = new Bitmap(width, height);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.White);
                var rect = new Rectangle(0, 0, width - 1, height - 1);

                switch (kind)
                {
                    case FlagKind.Italy:
                        FillVertical(g, rect, Color.FromArgb(0, 146, 70), Color.White, Color.FromArgb(206, 43, 55));
                        break;
                    case FlagKind.Germany:
                        FillHorizontal(g, rect, Color.Black, Color.FromArgb(221, 0, 0), Color.FromArgb(255, 206, 0));
                        break;
                    case FlagKind.France:
                        FillVertical(g, rect, Color.FromArgb(0, 85, 164), Color.White, Color.FromArgb(239, 65, 53));
                        break;
                    case FlagKind.Spain:
                        FillThreeBands(g, rect, Color.FromArgb(170, 21, 27), Color.FromArgb(241, 191, 0), Color.FromArgb(170, 21, 27), 0.25f, 0.5f);
                        break;
                    case FlagKind.Portugal:
                        using (var green = new SolidBrush(Color.FromArgb(4, 106, 56)))
                        using (var red = new SolidBrush(Color.FromArgb(218, 41, 28)))
                        {
                            var split = (int)Math.Round(rect.Width * 0.4);
                            g.FillRectangle(green, rect.Left, rect.Top, split, rect.Height);
                            g.FillRectangle(red, rect.Left + split, rect.Top, rect.Width - split, rect.Height);
                        }
                        break;
                    case FlagKind.Poland:
                        FillTwoBands(g, rect, Color.White, Color.FromArgb(220, 20, 60));
                        break;
                    case FlagKind.Netherlands:
                        FillHorizontal(g, rect, Color.FromArgb(174, 28, 40), Color.White, Color.FromArgb(33, 70, 139));
                        break;
                    case FlagKind.Turkey:
                        g.Clear(Color.FromArgb(227, 10, 23));
                        using (var white = new SolidBrush(Color.White))
                        using (var red = new SolidBrush(Color.FromArgb(227, 10, 23)))
                        {
                            g.FillEllipse(white, 6, 4, 10, 10);
                            g.FillEllipse(red, 9, 5, 8, 8);
                            g.FillEllipse(white, 17, 7, 3, 3);
                        }
                        break;
                    case FlagKind.Russia:
                        FillHorizontal(g, rect, Color.White, Color.FromArgb(0, 57, 166), Color.FromArgb(213, 43, 30));
                        break;
                    case FlagKind.Ukraine:
                        FillTwoBands(g, rect, Color.FromArgb(0, 87, 183), Color.FromArgb(255, 215, 0));
                        break;
                    case FlagKind.China:
                        g.Clear(Color.FromArgb(222, 41, 16));
                        using (var yellow = new SolidBrush(Color.FromArgb(255, 222, 0)))
                        {
                            g.FillEllipse(yellow, 4, 4, 5, 5);
                            g.FillEllipse(yellow, 10, 3, 2, 2);
                            g.FillEllipse(yellow, 12, 6, 2, 2);
                        }
                        break;
                    case FlagKind.Japan:
                        g.Clear(Color.White);
                        using (var red = new SolidBrush(Color.FromArgb(188, 0, 45)))
                            g.FillEllipse(red, width / 2 - 5, height / 2 - 5, 10, 10);
                        break;
                    case FlagKind.India:
                        FillHorizontal(g, rect, Color.FromArgb(255, 153, 51), Color.White, Color.FromArgb(19, 136, 8));
                        using (var blue = new SolidBrush(Color.FromArgb(0, 0, 128)))
                            g.FillEllipse(blue, width / 2 - 2, height / 2 - 2, 4, 4);
                        break;
                    case FlagKind.UnitedKingdom:
                        DrawUnitedKingdom(g, rect);
                        break;
                }

                using (var border = new Pen(Color.FromArgb(150, 150, 150)))
                    g.DrawRectangle(border, rect);
            }
            return bitmap;
        }

        private static void DrawUnitedKingdom(Graphics g, Rectangle rect)
        {
            g.Clear(Color.FromArgb(1, 33, 105));
            using (var whiteDiag = new Pen(Color.White, 5f))
            using (var redDiag = new Pen(Color.FromArgb(200, 16, 46), 2f))
            using (var whiteCross = new Pen(Color.White, 7f))
            using (var redCross = new Pen(Color.FromArgb(200, 16, 46), 4f))
            {
                g.DrawLine(whiteDiag, rect.Left, rect.Top, rect.Right, rect.Bottom);
                g.DrawLine(whiteDiag, rect.Right, rect.Top, rect.Left, rect.Bottom);
                g.DrawLine(redDiag, rect.Left, rect.Top, rect.Right, rect.Bottom);
                g.DrawLine(redDiag, rect.Right, rect.Top, rect.Left, rect.Bottom);
                g.DrawLine(whiteCross, rect.Left + rect.Width / 2, rect.Top, rect.Left + rect.Width / 2, rect.Bottom);
                g.DrawLine(whiteCross, rect.Left, rect.Top + rect.Height / 2, rect.Right, rect.Top + rect.Height / 2);
                g.DrawLine(redCross, rect.Left + rect.Width / 2, rect.Top, rect.Left + rect.Width / 2, rect.Bottom);
                g.DrawLine(redCross, rect.Left, rect.Top + rect.Height / 2, rect.Right, rect.Top + rect.Height / 2);
            }
        }

        private static void FillVertical(Graphics g, Rectangle rect, Color a, Color b, Color c)
        {
            var third = rect.Width / 3;
            using (var ba = new SolidBrush(a))
            using (var bb = new SolidBrush(b))
            using (var bc = new SolidBrush(c))
            {
                g.FillRectangle(ba, rect.Left, rect.Top, third, rect.Height);
                g.FillRectangle(bb, rect.Left + third, rect.Top, third, rect.Height);
                g.FillRectangle(bc, rect.Left + third * 2, rect.Top, rect.Width - third * 2, rect.Height);
            }
        }

        private static void FillHorizontal(Graphics g, Rectangle rect, Color a, Color b, Color c)
        {
            var third = rect.Height / 3;
            using (var ba = new SolidBrush(a))
            using (var bb = new SolidBrush(b))
            using (var bc = new SolidBrush(c))
            {
                g.FillRectangle(ba, rect.Left, rect.Top, rect.Width, third);
                g.FillRectangle(bb, rect.Left, rect.Top + third, rect.Width, third);
                g.FillRectangle(bc, rect.Left, rect.Top + third * 2, rect.Width, rect.Height - third * 2);
            }
        }

        private static void FillTwoBands(Graphics g, Rectangle rect, Color top, Color bottom)
        {
            var half = rect.Height / 2;
            using (var a = new SolidBrush(top))
            using (var b = new SolidBrush(bottom))
            {
                g.FillRectangle(a, rect.Left, rect.Top, rect.Width, half);
                g.FillRectangle(b, rect.Left, rect.Top + half, rect.Width, rect.Height - half);
            }
        }

        private static void FillThreeBands(Graphics g, Rectangle rect, Color top, Color middle, Color bottom, float topFraction, float middleFraction)
        {
            var h1 = (int)Math.Round(rect.Height * topFraction);
            var h2 = (int)Math.Round(rect.Height * middleFraction);
            using (var a = new SolidBrush(top))
            using (var b = new SolidBrush(middle))
            using (var c = new SolidBrush(bottom))
            {
                g.FillRectangle(a, rect.Left, rect.Top, rect.Width, h1);
                g.FillRectangle(b, rect.Left, rect.Top + h1, rect.Width, h2);
                g.FillRectangle(c, rect.Left, rect.Top + h1 + h2, rect.Width, rect.Height - h1 - h2);
            }
        }
    }
}
