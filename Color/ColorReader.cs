// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Globalization;
using Celeste.Core.Display;

namespace Celeste.Mods.FrostHelper.Color
{
    public sealed class ColorReader
    {
        private readonly Dictionary<string, PackedColor> namedColors = new(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, PackedColor[]> colorArrays = new(StringComparer.OrdinalIgnoreCase)
            { [""] = null };

        private readonly Action<string> invalidColor;

        public ColorReader(Action<string> invalidColor)
        {
            this.invalidColor = invalidColor;
            foreach (var property in typeof(PackedColor).GetProperties())
            {
                object value = property.GetValue(default(PackedColor), null);
                if (value is PackedColor color) namedColors[property.Name] = color;
            }

            namedColors[""] = PackedColor.White;
        }

        public bool TryRead(string value, out PackedColor color)
            => namedColors.TryGetValue(value, out color) || TryHex(value.AsSpan(), out color);

        public PackedColor Read(string value)
        {
            if (namedColors.TryGetValue(value, out PackedColor known)) return known;
            if (TryHex(value.AsSpan(), out PackedColor parsed)) return parsed;
            invalidColor("Invalid color: " + value);
            namedColors[value] = PackedColor.Transparent;
            return PackedColor.Transparent;
        }

        public PackedColor[] ReadArray(string value)
        {
            if (colorArrays.TryGetValue(value, out var colors)) return colors;
            var parts = value.Trim().Split(',');
            colors = new PackedColor[parts.Length];
            for (var i = 0; i < parts.Length; i++) colors[i] = Read(parts[i]);
            colorArrays[value] = colors;
            return colors;
        }

        public static bool TryHex(ReadOnlySpan<char> value, out PackedColor color)
        {
            if (value[0] == '#') value = value[1..];
            value = value.Trim();
            if (!uint.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint packed))
            {
                color = default;
                return false;
            }

            (bool valid, color) = value.Length switch
            {
                6 or 7 => (true, new PackedColor((byte)(packed >> 16), (byte)(packed >> 8), (byte)packed)),
                8 => (true,
                    new PackedColor((byte)(packed >> 24), (byte)(packed >> 16), (byte)(packed >> 8), (byte)packed)),
                _ => (false, default)
            };
            return valid;
        }
    }
}
