using System.Drawing;
using System.IO;
using System.Reflection;

namespace LegacySift
{
    internal static class AppIcon
    {
        internal const string ResourceName = "LegacySift.Assets.legacysift-icon.ico";

        public static Icon CreateIcon()
        {
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName))
            {
                if (stream == null) return (Icon)SystemIcons.Application.Clone();
                using (var icon = new Icon(stream))
                    return (Icon)icon.Clone();
            }
        }

        public static Bitmap CreateBitmap(int size)
        {
            using (var icon = CreateIcon())
            using (var scaled = new Icon(icon, new Size(size, size)))
                return scaled.ToBitmap();
        }
    }
}
