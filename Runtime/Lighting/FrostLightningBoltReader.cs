using System;
using System.Collections.Generic;
using System.Globalization;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Mods.FrostHelper.Color;
using Celeste.Mods.FrostHelper.Entities.Lighting;

namespace Celeste.Mods.FrostHelper.Runtime.Lighting
{
    public static class FrostLightningBoltReader
    {
        public static CustomLightningRenderer.Config.BoltConfig[] Read(EntityData data, ColorReader colors)
        {
            var text = data.Attr("edgeBolts", null);
            if (text == null)
                return (CustomLightningRenderer.Config.BoltConfig[])CustomLightningRenderer.DefaultBolts.Clone();
            var result = new List<CustomLightningRenderer.Config.BoltConfig>();
            var remaining = text.AsSpan();
            while (!remaining.IsEmpty)
            {
                var next = ReadUntil(ref remaining, ';');
                if (!TryParse(next, colors, out var bolt)) break;
                result.Add(bolt);
            }

            return result.ToArray();
        }

        public static bool TryParse(ReadOnlySpan<char> text, ColorReader colors,
            out CustomLightningRenderer.Config.BoltConfig result)
        {
            result = default;
            var colorText = ReadUntil(ref text, ',');
            if (!colors.TryRead(colorText.ToString(), out var color)) return false;
            float thickness = 1f, interval = .1f;
            if (!text.IsEmpty && !float.TryParse(ReadUntil(ref text, ','),
                    NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture,
                    out thickness)) return false;
            if (!text.IsEmpty && !float.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands,
                    CultureInfo.InvariantCulture, out interval)) return false;
            result = new CustomLightningRenderer.Config.BoltConfig(color, thickness, interval);
            return true;
        }

        private static ReadOnlySpan<char> ReadUntil(ref ReadOnlySpan<char> remaining, char delimiter)
        {
            int index = remaining.IndexOf(delimiter);
            if (index < 0)
            {
                var all = remaining;
                remaining = default;
                return all;
            }

            var result = remaining.Slice(0, index);
            remaining = remaining.Slice(index + 1);
            return result;
        }
    }
}
