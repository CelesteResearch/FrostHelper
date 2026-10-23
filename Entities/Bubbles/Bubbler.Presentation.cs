// SPDX-License-Identifier: MIT

using System;
using System.Numerics;
using Celeste.Core.Geometry;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Rendering;
using Celeste.Game.Execution.Levels;
using Celeste.Graphics.Animation;
using Celeste.Scene.Tracking;

namespace Celeste.Mods.FrostHelper.Entities.Bubbles
{
    public partial class Bubbler
    {
        public override void Render(ISourceRenderContext context)
        {
            base.Render(context);
            if (previewSprite == null || !IsPreviewVisible(previewSprite)) return;
            DrawDreamySprite(previewSprite, context);
        }

        private void DrawDreamySprite(Sprite image, ISourceRenderContext context)
        {
            int height = 0;
            while (height < image.Height)
            {
                image.DrawSubrect(context,
                    new Vector2((float)Math.Sin(
                        ((ISourceScene)engine.Frame.Scenes.CurrentScene).SourceScene.Clock.TimeActive * 2f +
                        height * .4f) * 1.3f, height),
                    new IntRect(0, height, (int)image.Width, 1),
                    ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
                height++;
            }
        }

        private bool IsPreviewVisible(Sprite image)
        {
            var scale = image.Scale;
            int width = (int)(image.Width * scale.X);
            int height = (int)(image.Height * scale.Y);
            var origin = image.Origin;
            float x = (int)image.RenderPosition.X - (int)origin.X / 2;
            float y = (int)image.RenderPosition.Y - (int)origin.Y / 2;
            var camera = ((OriginalLevelSceneRoot)engine.Frame.Scenes.CurrentScene).Camera;
            x -= camera.Left;
            y -= camera.Top;
            return x + width >= -4f && y + height >= -4f &&
                   x <= 4f + camera.Viewport.Width * camera.ZoomVector.X &&
                   y <= 4f + camera.Viewport.Height * camera.ZoomVector.Y;
        }
    }
}
