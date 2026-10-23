// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Graphics;
using Celeste.Graphics.Content;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Entities;
using Celeste.Scene.Scheduling;
using Celeste.Scene.Tracking;
using Celeste.Simulation.Presentation.Decals.Entities;
using Celeste.Simulation.Presentation.Lighting;
using Celeste.Simulation.Presentation.PostProcessing.Bloom;
using Celeste.Simulation.Gameplay.Platforms.StaticMovers;

namespace Celeste.Mods.FrostHelper.Entities.Decals
{
    public sealed class DecalContainerRenderer : SceneEntityState
    {
        public readonly List<DecalContainer> Containers = new();
        public readonly ParallaxDecalRenderer Parallax = new();
        private readonly ISourceEngineContext engine;

        public DecalContainerRenderer(ISourceEngineContext engine)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration) => this.engine = engine;

        public override void Awake(object scene)
        {
            foreach (var container in Containers) container.Awake(scene);
            base.Awake(scene);
        }

        public override void Render(ISourceRenderContext context)
        {
            var level = (OriginalLevelSceneRoot)Scene;
            var buffer = ((OriginalGameGraphicsResources)((ISourceGameGraphicsEnvironment)engine).GameGraphics).Buffers
                .Gameplay;
            foreach (var container in Containers) container.Render(level, context, buffer.Width, buffer.Height);
            Parallax.Render(level, context, buffer.Width, buffer.Height);
            base.Render(context);
        }
    }
}
