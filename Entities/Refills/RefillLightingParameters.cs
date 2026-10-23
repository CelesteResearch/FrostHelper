using System;
using System.Globalization;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Scheduling;
using Celeste.Mods.FrostHelper.Color;
using Celeste.Simulation.Presentation.Lighting;
using Celeste.Simulation.Presentation.PostProcessing.Bloom;

namespace Celeste.Mods.FrostHelper.Entities.Refills
{
    internal static class RefillLightingParameters
    {
        private static string Next(string text, ref int position)
        {
            int end = text.IndexOf(';', position);
            if (end < 0) end = text.Length;
            string value = text.Substring(position, end - position);
            position = end < text.Length ? end + 1 : end;
            return value;
        }

        public static BloomPointState Bloom(string text)
        {
            int position = 0;
            if (!string.IsNullOrWhiteSpace(text)
                && float.TryParse(Next(text, ref position), NumberStyles.Float | NumberStyles.AllowThousands,
                    CultureInfo.InvariantCulture, out float alpha)
                && float.TryParse(Next(text, ref position), NumberStyles.Float | NumberStyles.AllowThousands,
                    CultureInfo.InvariantCulture, out float radius))
                return new BloomPointState(alpha, radius);
            return new BloomPointState(.8f, 16f);
        }

        public static VertexLightState Light(ISourceEngineContext engine, string text, ColorReader colors)
        {
            int position = 0;
            if (!string.IsNullOrWhiteSpace(text)
                && colors.TryRead(Next(text, ref position), out PackedColor color)
                && float.TryParse(Next(text, ref position), NumberStyles.Float | NumberStyles.AllowThousands,
                    CultureInfo.InvariantCulture, out float alpha)
                && int.TryParse(Next(text, ref position), NumberStyles.Integer, CultureInfo.InvariantCulture,
                    out int start)
                && int.TryParse(Next(text, ref position), NumberStyles.Integer, CultureInfo.InvariantCulture,
                    out int end))
                return new VertexLightState(engine, Vector2.Zero, color, alpha, start, end);
            return new VertexLightState(engine, Vector2.Zero, PackedColor.White, 1f, 16, 40);
        }
    }
}
