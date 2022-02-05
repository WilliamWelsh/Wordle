using Discord;
using SixLabors.ImageSharp.PixelFormats;

namespace Wordle
{
    public static class Colors
    {
        // Discord Colors
        public static Color DiscordDarkGrey = new Color(18, 18, 19);
        public static Color DiscordGreen = new Color(83, 141, 78);
        public static Color DiscordRed = new Color(231, 76, 60);

        // Image Sharp Colors
        public static Rgba32 RgbaDarkGrey = new Rgba32(18, 18, 19);
        public static Rgba32 RgbaGrey = new Rgba32(58, 58, 60);
        public static Rgba32 RgbaLightGrey = new Rgba32(58, 58, 60);
        public static Rgba32 RgbaWhite = new Rgba32(215, 218, 220);
        public static Rgba32 RgbaGreen = new Rgba32(83, 141, 78);
        public static Rgba32 RgbaYellow = new Rgba32(181, 159, 59);
    }
}