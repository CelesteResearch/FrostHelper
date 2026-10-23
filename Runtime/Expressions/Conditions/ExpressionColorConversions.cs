using System;
using Celeste.Core.Display;

namespace Celeste.Mods.FrostHelper.Runtime.Expressions.Conditions
{
    internal static class ExpressionColorConversions
    {
        public static string ColorToHex(PackedColor color) => color.A == 255
            ? $"{color.R:x2}{color.G:x2}{color.B:x2}"
            : $"{color.R:x2}{color.G:x2}{color.B:X2}{color.A:x2}";

        public static int ColorToHexInt(PackedColor color) =>
            color.A | (color.B << 8) | (color.G << 16) | (color.R << 24);

        public static PackedColor HexToColorInt(int hexRgba) => new((byte)(hexRgba >> 24), (byte)(hexRgba >> 16),
            (byte)(hexRgba >> 8), (byte)hexRgba);

        public static PackedColor GetColor(string text, FrostConditionRuntime runtime) => runtime.Colors.Read(text);
    }
}
