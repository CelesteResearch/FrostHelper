using Celeste.Graphics.Geometry.TwoD.Camera;

namespace Celeste.Mods.FrostHelper.Runtime.Rendering
{
    internal static class FrostCameraCull
    {
        public static bool IsRectangleVisible(float x, float y, float width, float height, float lenience,
            CameraState camera)
        {
            x -= camera.Left;
            y -= camera.Top;
            return x + width >= -lenience && y + height >= -lenience &&
                   x <= lenience + camera.Viewport.Width * camera.ZoomVector.X &&
                   y <= lenience + camera.Viewport.Height * camera.ZoomVector.Y;
        }
    }
}
