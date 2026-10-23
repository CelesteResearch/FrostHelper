using System;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Randomness;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Rendering;
using Celeste.Scene.Entities;

namespace Celeste.Mods.FrostHelper.Entities.Platforms.SwapBlocks
{
    [RuntimeTypeAlias("FrostHelper.ToggleSwapBlock+PathRenderer")]
    internal sealed class ToggleSwapBlockPath : SceneEntityState
    {
        private readonly ToggleSwapBlock block;
        private readonly AtlasEntry pathTexture;
        private readonly AtlasEntry clipTexture = new();
        private float timer;

        public ToggleSwapBlockPath(ToggleSwapBlock block) : base(((ISourceEntitySourceDataEnvironment)block.Engine)
            .EntitySourceDataRegistration)
        {
            this.block = block;
            Depth = 8999;
            pathTexture = block.GameAtlas[block.directory + "/path" + (block.start.X == block.end.X ? "V" : "H")];
            timer = SourceRandomMath.NextFloat(((ISourceRandomEnvironment)block.Engine).Random);
        }

        public override void Update()
        {
            base.Update();
            timer += block.Engine.DeltaTime * 4f;
        }

        public override void Render(ISourceRenderContext context)
        {
            if (!block.IsVisible(block.moveRect.X, block.moveRect.Y, block.moveRect.Width,
                    block.moveRect.Height)) return;
            for (int x = block.moveRect.Left; x < block.moveRect.Right; x += pathTexture.FrameWidth)
            for (int y = block.moveRect.Top; y < block.moveRect.Bottom; y += pathTexture.FrameHeight)
            {
                pathTexture.GetSubtexture(0, 0, Math.Min(pathTexture.FrameWidth, block.moveRect.Right - x),
                    Math.Min(pathTexture.FrameHeight, block.moveRect.Bottom - y), clipTexture);
                if (block.renderBG)
                    SourceAtlasDrawing.DrawCentered(context, this, clipTexture,
                        new Vector2(x + clipTexture.FrameWidth / 2, y + clipTexture.FrameHeight / 2),
                        PackedColor.White);
            }

            float scale = .5f * (.5f + ((float)Math.Sin(timer) + 1f) * .25f);
            block.DrawBlockStyle(context, new Vector2(block.moveRect.X, block.moveRect.Y), block.moveRect.Width,
                block.moveRect.Height, block.nineSliceTarget, null, PackedColor.White * scale);
        }
    }
}
