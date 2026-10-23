// SPDX-License-Identifier: MIT

using System;
using System.Numerics;

namespace Celeste.Mods.FrostHelper.Rendering.Dream
{
    internal static class DreamParticleRendering
    {
        internal static int Frame(float timeOffset, float timer, int[] frames)
            => frames[(int)((timeOffset * (double)frames.Length + timer) % frames.Length)];

        internal static Vector2 Wrap(Vector2 position, int left, int top, int width, int height)
        {
            int right = left + width, bottom = top + height;
            if (position.X > right) position.X -= (float)Math.Ceiling((position.X - right) / width) * width;
            else if (position.X < left) position.X += (float)Math.Ceiling((left - position.X) / width) * width;
            if (position.Y > bottom) position.Y -= (float)Math.Ceiling((position.Y - bottom) / height) * height;
            else if (position.Y < top) position.Y += (float)Math.Ceiling((top - position.Y) / height) * height;
            return position;
        }
    }
}
