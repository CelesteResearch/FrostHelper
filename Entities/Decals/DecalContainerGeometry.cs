using System;
using System.Numerics;
using Celeste.Core.Mathematics;
using Celeste.Simulation.Presentation.Decals.Entities;

namespace Celeste.Mods.FrostHelper.Entities.Decals
{
    public static class DecalContainerGeometry
    {
        public static DecalInfo CreateInfo(SourceDecalEntityState item)
        {
            var texture = item.Textures[0];
            float width = texture.FrameWidth * Math.Abs(item.Scale.X);
            float height = texture.FrameHeight * Math.Abs(item.Scale.Y);

            if (item.Rotation == 0f)
                return new DecalInfo { Decal = item, HalfWidth = width / 2f, HalfHeight = height / 2f };

            var size = new Vector2(width, height);
            var offset = new Vector2(-.5f, -.5f) * size;
            var vectors = ((ISourceVectorEnvironment)item.EngineContext).VectorArithmetic;
            var p1 = vectors.Rotate(offset, item.Rotation);
            var p2 = vectors.Rotate(offset + new Vector2(size.X, 0f), item.Rotation);
            var p3 = vectors.Rotate(offset + new Vector2(0f, size.Y), item.Rotation);
            var p4 = vectors.Rotate(offset + size, item.Rotation);

            var minimum = new Vector2(
                Math.Min(p4.X, Math.Min(p3.X, Math.Min(p1.X, p2.X))),
                Math.Min(p4.Y, Math.Min(p3.Y, Math.Min(p1.Y, p2.Y))));
            var maximum = new Vector2(
                Math.Max(p4.X, Math.Max(p3.X, Math.Max(p1.X, p2.X))),
                Math.Max(p4.Y, Math.Max(p3.Y, Math.Max(p1.Y, p2.Y))));

            // RectangleExt.FromPoints truncates each endpoint before subtracting.
            int rectangleWidth = unchecked((int)maximum.X - (int)minimum.X);
            int rectangleHeight = unchecked((int)maximum.Y - (int)minimum.Y);
            return new DecalInfo
            {
                Decal = item,
                HalfWidth = rectangleWidth / 2f,
                HalfHeight = rectangleHeight / 2f
            };
        }

        public static bool IsInside(Vector2 camera, DecalInfo decal, int gameplayWidth, int gameplayHeight)
        {
            float halfWidth = decal.HalfWidth;
            float halfHeight = decal.HalfHeight;
            float x = decal.Decal.Position.X;
            float y = decal.Decal.Position.Y;

            return x + halfWidth >= camera.X - halfWidth
                   && x - halfWidth <= camera.X + gameplayWidth + halfWidth
                   && y + halfHeight >= camera.Y - halfHeight
                   && y - halfHeight <= camera.Y + gameplayHeight + halfHeight;
        }

        public static bool ContainerInside(Vector2 position, float width, float height, Vector2 camera,
            int gameplayWidth, int gameplayHeight)
        {
            const float lenience = 64f;
            return position.X + width >= camera.X - lenience
                   && position.X <= camera.X + gameplayWidth + lenience
                   && position.Y + height >= camera.Y - lenience
                   && position.Y <= camera.Y + gameplayHeight + lenience;
        }
    }
}
