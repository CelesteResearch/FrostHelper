using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Rendering;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Rendering.Effects;

namespace Celeste.Mods.FrostHelper.Spinners.Rendering
{
    public sealed class SpinnerRenderImage
    {
        public Func<float, AtlasEntry> TextureAt;
        public Vector2 Position;
        public Vector2 Origin;
        public float Rotation;
        public PackedColor Color = PackedColor.White;

        public SpinnerRenderImage(Func<float, AtlasEntry> textureAt)
            => TextureAt = textureAt ?? throw new ArgumentNullException(nameof(textureAt));
    }
}
