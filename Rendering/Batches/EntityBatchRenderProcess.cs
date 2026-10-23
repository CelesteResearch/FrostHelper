// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using Celeste.Core.Framework.Timing;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using Celeste.Core.Display;
using Celeste.Core.Rendering;
using Celeste.Core.Rendering.States;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Graphics;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Presentation.Rendering;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.VirtualAssets;
using Celeste.Graphics.Effects.Blur;
using Celeste.Graphics.Rendering.Devices;
using Celeste.Graphics.Rendering.Drawing;
using Celeste.Graphics.Rendering.Effects;
using Celeste.Mods.FrostHelper.Entities.Rendering;
using Celeste.Scene.Entities;

namespace Celeste.Mods.FrostHelper.Rendering.Batches
{
    internal sealed partial class EntityBatchRenderProcess : IDisposable
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, EntityBatchRenderProcess> Owners = new();

        internal static EntityBatchRenderProcess For(ISourceEngineContext engine) =>
            Owners.GetValue(engine, static e => new(e));

        internal static void Release(ISourceEngineContext engine)
        {
            if (!Owners.TryGetValue(engine, out var owner)) return;
            owner.Dispose();
            Owners.Remove(engine);
        }

        private readonly ISourceEngineContext engine;
        private readonly ISourceVirtualContentEnvironment content;
        private readonly ISourceGraphicsDeviceEnvironment device;
        private readonly ISourceDrawScope draw;
        private readonly ISourceGaussianBlurEnvironment blur;
        private readonly OriginalGameGraphicsResources graphics;
        private VirtualRenderTarget temp;
        private VirtualRenderTarget temp2;

        private EntityBatchRenderProcess(ISourceEngineContext engine)
        {
            this.engine = engine;
            content = (ISourceVirtualContentEnvironment)engine;
            device = (ISourceGraphicsDeviceEnvironment)engine;
            draw = (ISourceDrawScope)engine;
            blur = (ISourceGaussianBlurEnvironment)engine;
            graphics = (OriginalGameGraphicsResources)((ISourceGameGraphicsEnvironment)engine).GameGraphics;
        }

        private OriginalLevelSceneRoot CurrentLevel => (OriginalLevelSceneRoot)engine.Frame.Scenes.CurrentScene;

        private static GaussianBlurSamples GetSamples(Dictionary<string, object> parameters)
        {
            object value = parameters["samples"];
            if (value is string text)
            {
                Enum.TryParse<GaussianBlurSamples>(text, true, out var samples);
                parameters["samples"] = samples;
                return samples;
            }

            return (GaussianBlurSamples)value;
        }

        private static float GetFloat(string name, Dictionary<string, object> parameters)
        {
            object value = parameters[name];
            if (value is string text)
            {
                float number = Convert.ToSingle(text, CultureInfo.InvariantCulture);
                parameters[name] = number;
                return number;
            }

            return (float)value;
        }

        internal void Apply(ISourceRenderContext context, List<SceneEntityState> entities, string shader,
            Dictionary<string, object> parameters, int? depth, bool consumeStylegrounds, bool makeInvisible)
        {
            var diagnostics = SourceFrameDiagnostics.Current;
            long entryFlushStarted = 0;
            bool measuringEntryFlush = diagnostics?.BeginCustomBatchEntryFlush(out entryFlushStarted) ?? false;
            try
            {
                draw.Draw.SpriteBatch.End();
            }
            finally
            {
                if (measuringEntryFlush) diagnostics.EndCustomBatchEntryFlush(entryFlushStarted);
            }

            var gameplay = graphics.Buffers.Gameplay;
            if (temp == null || temp.Width != gameplay.Width)
            {
                temp?.Dispose();
                temp2?.Dispose();
                temp = content.VirtualContent.CreateRenderTarget("fh.entitybatcher.Temp", gameplay.Width,
                    gameplay.Height);
                temp2 = content.VirtualContent.CreateRenderTarget("fh.entitybatcher.Temp2", gameplay.Width,
                    gameplay.Height);
            }

            device.GraphicsDevice.SetRenderTarget(graphics.Buffers.TempB.Target);
            device.GraphicsDevice.Clear(PackedColor.Transparent);
            var captureDiagnostics = SourceFrameDiagnostics.Current;
            long captureStarted = 0;
            bool measuringCapture = captureDiagnostics?.BeginCustomBatchCapture(out captureStarted) ?? false;
            try
            {
                GameplayRendererState.Begin(context);
                long entityRenderStarted = 0;
                bool measuringEntityRender =
                    captureDiagnostics?.BeginCustomBatchEntityRender(out entityRenderStarted) ?? false;
                try
                {
                    EntityBatcher.RenderCapturedEntities(context, entities, depth, makeInvisible);
                }
                finally
                {
                    if (measuringEntityRender) captureDiagnostics.EndCustomBatchEntityRender(entityRenderStarted);
                }

                GameplayRendererState.End(context);
            }
            finally
            {
                if (measuringCapture) captureDiagnostics.EndCustomBatchCapture(captureStarted);
            }

            switch (shader)
            {
                case "mask":
                    RenderMask(context, parameters, consumeStylegrounds, false);
                    return;
                case "blurMask":
                    RenderMask(context, parameters, consumeStylegrounds, true);
                    return;
                case "vanilla.gaussianBlur":
                    GaussianBlurPresentation.Blur(blur, graphics.Buffers.TempB.Target, temp, gameplay,
                        GetFloat("fade", parameters), false, GetSamples(parameters), GetFloat("alpha", parameters));
                    GameplayRendererState.Begin(context);
                    return;
            }

            var effect = GetEffect(shader);
            ApplyParameters(effect, parameters, true);
            SimpleApply(graphics.Buffers.TempB, gameplay, effect);
            device.GraphicsDevice.SetRenderTarget(gameplay.Target);
            GameplayRendererState.Begin(context);
        }

        private void DrawMask(Dictionary<string, object> parameters, bool consumeStylegrounds)
        {
            var diagnostics = SourceFrameDiagnostics.Current;
            long maskStarted = 0;
            bool measuringMask = diagnostics?.BeginCustomBatchMask(out maskStarted) ?? false;
            try
            {
                device.GraphicsDevice.SetRenderTarget(temp2.Target);
                device.GraphicsDevice.Clear(PackedColor.Transparent);
                if (parameters.ContainsKey("renderBgStylegrounds"))
                {
                    var level = CurrentLevel;
                    if (consumeStylegrounds)
                        foreach (var backdrop in level.Background.Backdrops)
                            backdrop.Visible = true;
                    level.Background.Render(level);
                    if (consumeStylegrounds)
                        foreach (var backdrop in level.Background.Backdrops)
                            backdrop.Visible = false;
                }

                var batch = draw.Draw.SpriteBatch;
                batch.Begin();
                batch.Draw(graphics.Buffers.Gameplay.Target, Vector2.Zero, PackedColor.White.PackedValue);
                batch.End();
                if (parameters.ContainsKey("renderEntitiesToGameplay")) Redraw(parameters, true);
                device.GraphicsDevice.SetRenderTarget(null);
            }
            finally
            {
                if (measuringMask) diagnostics.EndCustomBatchMask(maskStarted);
            }
        }

        private void Redraw(Dictionary<string, object> parameters, bool force = false)
        {
            if (!force && !parameters.ContainsKey("rerenderEntities")) return;
            var batch = draw.Draw.SpriteBatch;
            batch.Begin(SourceSpriteSortMode.Deferred, SourceBlendState.AlphaBlend, SourceSamplerState.PointClamp,
                SourceDepthStencilState.Default, SourceRasterizerState.CullNone, null);
            batch.Draw(graphics.Buffers.TempB.Target, Vector2.Zero, PackedColor.White.PackedValue);
            batch.End();
        }

        private void RenderMask(ISourceRenderContext context, Dictionary<string, object> parameters,
            bool consumeStylegrounds, bool applyBlur)
        {
            DrawMask(parameters, consumeStylegrounds);
            var diagnostics = SourceFrameDiagnostics.Current;
            long filterStarted = 0;
            bool measuringFilter = diagnostics?.BeginCustomBatchFilter(out filterStarted) ?? false;
            try
            {
                var source = temp2;
                if (applyBlur)
                {
                    GaussianBlurPresentation.Blur(blur, temp2.Target, temp, graphics.Buffers.TempA,
                        GetFloat("fade", parameters), false, GetSamples(parameters), 1f,
                        GaussianBlurDirection.Both, GetFloat("alpha", parameters));
                    source = graphics.Buffers.TempA;
                }

                object value = parameters["maskShader"];
                SourceEffect effect;
                if (value is string name) parameters["maskShader"] = effect = GetEffect(name);
                else
                    effect = value as SourceEffect ??
                             throw new Exception("Mask shaders need a 'maskShader' parameter!");
                ApplyParameters(effect, parameters, false);
                device.GraphicsDevice.Textures[1] = graphics.Buffers.TempB.Target;
                SimpleApply(source, graphics.Buffers.Gameplay, effect);
                Redraw(parameters);
                GameplayRendererState.Begin(context);
            }
            finally
            {
                if (measuringFilter) diagnostics.EndCustomBatchFilter(filterStarted);
            }
        }

        private void SimpleApply(VirtualRenderTarget source, VirtualRenderTarget destination, SourceEffect effect)
        {
            ApplyStandardParameters(effect);
            device.GraphicsDevice.SetRenderTarget(destination.Target);
            var batch = draw.Draw.SpriteBatch;
            batch.Begin(SourceSpriteSortMode.Deferred, SourceBlendState.AlphaBlend, SourceSamplerState.PointClamp,
                SourceDepthStencilState.Default, SourceRasterizerState.CullNone, effect);
            batch.Draw(source.Target, Vector2.Zero, PackedColor.White.PackedValue);
            batch.End();
        }

        public void Dispose()
        {
            temp?.Dispose();
            temp2?.Dispose();
            temp = temp2 = null;
            foreach (var effect in effects.Values) effect.Dispose();
            effects.Clear();
        }
    }
}
