using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Components.Rendering;
using Celeste.Core.Display;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Presentation.Rendering;
using Celeste.Graphics.Content.VirtualAssets;
using Celeste.Graphics.Effects.Outlines;
using Celeste.Graphics.Rendering.Devices;
using Celeste.Graphics.Rendering.Drawing;
using Celeste.Scene.Entities;

namespace Celeste.Mods.FrostHelper.Rendering.Debris
{
    internal sealed class BatchedDebrisOutlineRenderer : SceneEntityState
    {
        private readonly ISourceEngineContext engine;
        private readonly Action endBatch;
        private readonly Action beginGameplay;
        private readonly Action drawImages;
        private readonly Action recoverBatch;
        private BufferedFourOffsetOutline outline;
        private ISourceRenderContext captureContext;
        internal readonly List<BatchedDebrisOutline> Images = new();
        internal PackedColor Color { get; }

        internal BatchedDebrisOutlineRenderer(ISourceEngineContext engine, int depth, PackedColor color)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration)
        {
            this.engine = engine;
            Depth = depth;
            Color = color;
            endBatch = EndBatch;
            beginGameplay = BeginGameplay;
            drawImages = DrawImages;
            recoverBatch = RecoverBatch;
            Add(new BeforeRenderCallbackComponentState(BeforeRender));
        }

        private void EndBatch()
        {
            if (((ISourceDrawScope)engine).Draw.SpriteBatch.IsBatchOpen) captureContext.EndBatch();
        }

        private void BeginGameplay() => GameplayRendererState.Begin(captureContext);

        private static void RecoverBatch()
        {
        }

        private void DrawImages()
        {
            foreach (var item in Images) item.Image.Render(captureContext);
        }

        private void BeforeRender()
        {
            captureContext = ((ISourceRenderContextScene)Scene).SourceRenderContext;
            outline ??= new BufferedFourOffsetOutline(((ISourceVirtualContentEnvironment)engine).VirtualContent,
                ((ISourceGraphicsDeviceEnvironment)engine).GraphicsDevice, ((ISourceDrawScope)engine).Draw.SpriteBatch,
                "frost.debris-outline." + Depth);
            outline.Capture(320, 180, endBatch, beginGameplay, drawImages, recoverBatch);
        }

        public override void Render(ISourceRenderContext context)
        {
            base.Render(context);
            var camera = ((OriginalLevelSceneRoot)Scene).Camera;
            outline?.RenderCaptured(new Vector2(MathF.Floor(camera.Position.X), MathF.Floor(camera.Position.Y)), Color,
                true);
        }

        public override void SceneEnd(object scene)
        {
            outline?.Dispose();
            outline = null;
            base.SceneEnd(scene);
        }
    }
}
