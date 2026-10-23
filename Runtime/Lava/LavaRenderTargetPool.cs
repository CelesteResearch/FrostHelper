// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Mathematics.LinearAlgebra;
using Celeste.Core.Rendering;
using Celeste.Core.Rendering.States;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Environment.Lava;
using Celeste.Graphics.Content.VirtualAssets;
using Celeste.Graphics.Rendering.Devices;
using Celeste.Graphics.Rendering.Drawing;
using Celeste.Graphics.Rendering.Primitives;
using Celeste.Graphics.Rendering.Vertices;
using Celeste.Graphics.Rendering.Textures;

namespace Celeste.Mods.FrostHelper.Runtime.Lava
{
    internal sealed class LavaRenderTargetPool : IDisposable
    {
        private readonly ISourceEngineContext engine;
        internal LavaRenderTargetPool(ISourceEngineContext engine) => this.engine = engine;
        private readonly Stack<VirtualRenderTarget> available = new();

        private readonly SourceRenderTargetBinding[][] bindings =
        [
            [], new SourceRenderTargetBinding[1], new SourceRenderTargetBinding[2],
            new SourceRenderTargetBinding[3], new SourceRenderTargetBinding[4]
        ];

        private int created;

        internal void Draw(ISourceRenderContext context, IOriginalLavaProcess lavaEngine,
            OriginalLevelSceneRoot level, Vector2 position, SourceVertexPositionColor[] vertices, int count)
        {
            var device = ((ISourceGraphicsDeviceEnvironment)engine).GraphicsDevice;
            var content = ((ISourceVirtualContentEnvironment)engine).VirtualContent;
            var batch = ((ISourceDrawScope)engine).Draw.SpriteBatch;
            var gameplay = level.Buffers.Gameplay;
            VirtualRenderTarget target = null;
            while (available.Count != 0)
            {
                target = available.Pop();
                if (target.Width == gameplay.Width && !target.IsDisposed) break;
                target.Dispose();
                target = null;
            }

            target ??= content.CreateRenderTarget("FrostHelper.Lava[" + created++ + "]", gameplay.Width,
                gameplay.Height, false, true, 0);
            var previous = bindings[device.GetRenderTargetsNoAllocEXT(null)];
            device.GetRenderTargetsNoAllocEXT(previous);
            var viewport = device.Viewport;
            var scissor = device.ScissorRectangle;
            device.SetRenderTarget(target.Target);
            device.Clear(PackedColor.Transparent);
            SourcePrimitiveGraphics.DrawVertices(lavaEngine.PrimitiveGraphics,
                SourceMatrixMath.Multiply(SourceMatrixMath.CreateTranslation(new Vector3(position, 0)),
                    level.Camera.Matrix), vertices, count, null, SourceBlendState.Opaque);
            device.SetRenderTargets(previous);
            device.Viewport = viewport;
            device.ScissorRectangle = scissor;
            lavaEngine.GameplayBatchScope.Begin(context);
            batch.Draw(target.Target, level.Camera.Position, PackedColor.White.PackedValue);
            available.Push(target);
        }

        public void Dispose()
        {
            while (available.Count != 0) available.Pop().Dispose();
        }
    }
}
