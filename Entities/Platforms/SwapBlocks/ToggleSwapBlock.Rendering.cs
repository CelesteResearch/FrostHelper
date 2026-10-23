using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Rendering;
using Celeste.Game.Entities.Platforms.SwapBlocks.Rendering;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content.Atlas;

namespace Celeste.Mods.FrostHelper.Entities.Platforms.SwapBlocks
{
    public partial class ToggleSwapBlock
    {
        public override void Render(ISourceRenderContext context)
        {
            Vector2 vector = Position + Shake;
            if (lerp != target && speed > 0f)
            {
                Vector2 value = Vectors.SafeNormalize(end - start);
                if (target == 1)
                {
                    value *= -1f;
                }

                float num = speed / maxForwardSpeed;
                float num2 = 16f * num;
                int num3 = 2;
                while (num3 < num2)
                {
                    DrawBlockStyle(context, vector + value * num3, Width, Height, nineSliceGreen, middleGreen,
                        PackedColor.White * (1f - num3 / num2));
                    num3 += 2;
                }
            }

            if (redAlpha < 1f)
            {
                DrawBlockStyle(context, vector, Width, Height, nineSliceGreen, middleGreen, PackedColor.White);
            }

            if (redAlpha > 0f)
            {
                DrawBlockStyle(context, vector, Width, Height, nineSliceRed, middleRed, PackedColor.White * redAlpha);
            }
        }

        internal void DrawBlockStyle(ISourceRenderContext context, Vector2 pos, float width, float height,
            AtlasEntry[,] slices, Sprite middle, PackedColor color)
        {
            if (!IsVisible(pos.X, pos.Y, width, height)) return;
            SwapBlockDrawing.DrawBlockStyle(context, this, pos, width, height, slices, middle, color);
        }
    }
}
