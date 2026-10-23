using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Components;
using Celeste.Core.Rendering;
using Celeste.Core.Rendering.States;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Startup;
using Celeste.Game.Presentation.Rendering;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Content.VirtualAssets;
using Celeste.Graphics.Effects.Outlines;
using Celeste.Graphics.Rendering;
using Celeste.Graphics.Rendering.Batches;
using Celeste.Graphics.Rendering.Devices;
using Celeste.Graphics.Rendering.Drawing;
using Celeste.Scene.Entities;

namespace Celeste.Mods.FrostHelper.Spinners.Rendering
{
    public sealed class SpinnerBorderRenderer : SpinnerRenderer, IDisposable
    {
        private readonly SpinnerConnectorRenderer connector;
        private BufferedFourOffsetOutline outline;
        private readonly Action endBatch;
        private readonly Action beginGameplay;
        private readonly Action beginOutline;
        private readonly Action drawSprites;
        private ISourceRenderContext currentContext;
        private ICustomSpinnerRenderSettings currentSettings;

        public SpinnerBorderRenderer(ISourceEngineContext engine, int depth,
            Func<ICustomSpinnerRenderSettings> settings, SpinnerConnectorRenderer connector)
            : base(engine, depth, depth + 2, settings)
        {
            this.connector = connector;
            endBatch = EndBatch;
            beginGameplay = BeginGameplay;
            beginOutline = BeginOutline;
            drawSprites = DrawTargetSprites;
        }

        public override void Render(ISourceRenderContext context)
        {
            if (Items.Count == 0) return;
            currentContext = context;
            currentSettings = Settings();
            if (currentSettings.CanUseRenderTargetRender)
            {
                outline ??= new BufferedFourOffsetOutline(
                    ((ISourceVirtualContentEnvironment)Engine).VirtualContent,
                    ((ISourceGraphicsDeviceEnvironment)Engine).GraphicsDevice, Batch,
                    "frost.spinner-outline." + BaseDepth);
                var camera = ((OriginalLevelSceneRoot)Scene).Camera;
                bool useImageColor = currentSettings.OutlineShader != null ||
                                     currentSettings.CanUseBlackOutlineRenderTargetOpt;
                var output = new Vector2(MathF.Floor(camera.Position.X), MathF.Floor(camera.Position.Y));
                outline.Render(320, 180, output,
                    useImageColor ? currentSettings.FirstBorderColor.Value : PackedColor.White,
                    currentSettings.CanUseBlackOutlineRenderTargetOpt,
                    currentSettings.OutlineShader != null,
                    endBatch, beginGameplay, drawSprites, beginOutline, beginGameplay);
                return;
            }

            if (currentSettings.OutlineShader != null)
            {
                EndBatch();
                BeginOutline();
                NormalRender();
                EndBatch();
                BeginGameplay();
            }
            else NormalRender();
        }

        private void EndBatch() => GameplayRendererState.End(currentContext);
        private void BeginGameplay() => GameplayRendererState.Begin(currentContext);

        private void BeginOutline()
        {
            if (currentSettings.OutlineShader == null)
            {
                BeginGameplay();
                return;
            }

            var camera = ((OriginalLevelSceneRoot)Scene).Camera;
            currentContext.BeginBatch(new SourceRenderBatchState(SourceSpriteSortMode.Deferred,
                SourceBlendState.AlphaBlend, SourceSamplerState.PointClamp, SourceDepthStencilState.None,
                SourceRasterizerState.CullNone, currentSettings.OutlineShader, camera.Matrix));
        }

        private void DrawTargetSprites()
        {
            bool useImageColor = currentSettings.OutlineShader != null ||
                                 currentSettings.CanUseBlackOutlineRenderTargetOpt;
            PackedColor? color = useImageColor ? null : currentSettings.FirstBorderColor;
            connector?.ForceRender(color);
            float time = Items[Items.Count - 1].TimeUnpaused;
            foreach (var spinner in Items) DrawForeground(spinner, color, time);
        }

        private void NormalRender()
        {
            foreach (var spinner in Items)
            {
                var images = spinner.Sprites;
                for (int i = 0; i < images.Count; i++)
                {
                    var image = images[i];
                    DrawBorder(spinner, image, image.TextureAt(spinner.TimeUnpaused), false);
                }

                var fills = spinner.ConnectionFills;
                if (fills.Count == 0) continue;
                var texture = fills[0].TextureAt(spinner.TimeUnpaused);
                for (int i = 0; i < fills.Count; i++) DrawBorder(spinner, fills[i], texture, true);
            }
        }

        private void DrawBorder(ICustomSpinnerRenderSource spinner, SpinnerRenderImage image,
            AtlasEntry texture, bool centerOrigin)
        {
            var color = spinner.BorderColor;
            float fix = texture.SourceDrawScale;
            Vector2 origin = ((centerOrigin ? texture.Center : image.Origin) - texture.DrawOffset) / fix;
            var batch = Batch;
            var resource = texture.ReadTextureForSpriteBatch();
            Vector2 position = spinner.Position + (centerOrigin ? spinner.ShakeAmt : Vector2.Zero) + image.Position;
            var clip = texture.ClipRect;
            float rotation = image.Rotation;
            float scale = fix * spinner.ImageScale;
            batch.Draw(resource, position + -Vector2.UnitY, clip, color.PackedValue, rotation, origin,
                scale, SourceSpriteEffects.None, 0f);
            batch.Draw(resource, position + Vector2.UnitY, clip, color.PackedValue, rotation, origin,
                scale, SourceSpriteEffects.None, 0f);
            batch.Draw(resource, position + -Vector2.UnitX, clip, color.PackedValue, rotation, origin,
                scale, SourceSpriteEffects.None, 0f);
            batch.Draw(resource, position + Vector2.UnitX, clip, color.PackedValue, rotation, origin,
                scale, SourceSpriteEffects.None, 0f);
        }

        public void RenderForeground(ICustomSpinnerRenderSource spinner)
        {
            var settings = Settings();
            if (settings.CanUseRenderTargetRender && settings.CanUseBlackOutlineRenderTargetOpt) return;
            DrawForeground(spinner, null, spinner.TimeUnpaused);
        }

        public void Dispose() => outline?.Dispose();

        public override void Removed(object scene)
        {
            Dispose();
            base.Removed(scene);
        }

        public override void SceneEnd(object scene)
        {
            Dispose();
            base.SceneEnd(scene);
        }
    }
}
