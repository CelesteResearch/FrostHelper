using System;
using System.Collections.Generic;
using System.Globalization;
using Celeste.Core.Mathematics;
using Celeste.Physics.Collision.Shapes;

namespace Celeste.Mods.FrostHelper.Collider.Spinners
{
    public static class SpinnerHitboxReader
    {
        public static ColliderGeometry Create(string text, float scale, ISourceVectorArithmetic arithmetic,
            Action<string> warning)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            if (!(scale > 0f) || float.IsInfinity(scale))
            {
                warning?.Invoke("Invalid hitbox scale in " + text);
                return null;
            }

            var result = new List<ColliderGeometry>();
            string[] entries = text.Split(';');
            for (int i = 0; i < entries.Length; i++)
            {
                if (i == entries.Length - 1 && entries[i].Length == 0) break;
                string[] parts = entries[i].Split(',');
                bool rectangle = parts[0] == "R";
                bool circle = parts[0] == "C";
                if (!rectangle && !circle)
                {
                    warning?.Invoke("Invalid hitbox type in " + text);
                    return null;
                }

                string[] names = rectangle ? new[] { "width", "height", "x", "y" } : new[] { "radius", "x", "y" };
                int[] values = new int[names.Length];
                for (int j = 0; j < names.Length; j++)
                {
                    if (parts.Length > j + 1 && int.TryParse(parts[j + 1], NumberStyles.Integer,
                            CultureInfo.InvariantCulture, out values[j])) continue;
                    warning?.Invoke("Invalid " + (rectangle ? "rectangle " : "circle ") + names[j] + " in " + text);
                    return null;
                }

                result.Add(rectangle
                    ? new HitboxColliderGeometry(values[0] * scale, values[1] * scale, values[2] * scale,
                        values[3] * scale)
                    : new CircleColliderGeometry(arithmetic, values[0] * scale, values[1] * scale, values[2] * scale));
            }

            return result.Count == 1 ? result[0] : new ColliderListGeometry(result.ToArray());
        }

        public static ColliderGeometry Default(float scale, ISourceVectorArithmetic arithmetic)
            => new ColliderListGeometry(new CircleColliderGeometry(arithmetic, 6f * scale),
                new HitboxColliderGeometry(16f * scale, 4f * scale, -8f * scale, -3f * scale));
    }
}
