using UnityEngine;

namespace TrayMatch.View
{
    // Colors from the web prototype. Block colors are in the same order as COLORS there,
    // so palette index N in a level file means the same color in both.
    public static class Palette
    {
        static readonly Color[] BlockColors =
        {
            Hex(0xff6b6b), // red
            Hex(0x4dabf7), // blue
            Hex(0x51cf66), // green
            Hex(0xfcc419), // yellow
            Hex(0xcc5de8), // purple
            Hex(0xff922b), // orange
        };

        public static readonly Color Background = Hex(0x1e2130);
        public static readonly Color Board = Hex(0x262a3d);
        public static readonly Color Dot = Hex(0x343954);

        public static Color Block(int index) => BlockColors[index % BlockColors.Length];

        // amount < 0 darkens, e.g. -0.35 for block shadows.
        public static Color Shade(Color c, float amount) =>
            new Color(c.r * (1 + amount), c.g * (1 + amount), c.b * (1 + amount), c.a);

        static Color Hex(int rgb) =>
            new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);
    }
}