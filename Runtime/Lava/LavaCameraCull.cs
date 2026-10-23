// SPDX-License-Identifier: MIT

using System;
using Celeste.Core.Geometry;
using Celeste.Graphics.Geometry.TwoD.Camera;

namespace Celeste.Mods.FrostHelper.Runtime.Lava
{
    internal static class LavaCameraCull
    {
        internal static bool IsRectangleVisible(IntRect rectangle, float lenience, CameraState camera)
        {
            return global::Celeste.Mods.FrostHelper.Runtime.Rendering.FrostCameraCull.IsRectangleVisible(
                rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height, lenience, camera);
        }

        internal static IntRect GetVisibleSection(IntRect rectangle, int lenience, CameraState camera)
        {
            int x = Math.Max(rectangle.X, (int)camera.Left - lenience);
            int y = Math.Max(rectangle.Y, (int)camera.Top - lenience);
            return new IntRect(x, y, Math.Min(rectangle.Right, (int)camera.Right + lenience) - x,
                Math.Min(rectangle.Bottom, (int)camera.Bottom + lenience) - y);
        }
    }
}
