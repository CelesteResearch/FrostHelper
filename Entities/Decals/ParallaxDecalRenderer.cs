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
    public sealed class ParallaxDecalRenderer
    {
        internal readonly List<DecalInfo> Decals = new();

        public void AddDecal(SourceDecalEntityState decal)
        {
            Decals.Add(DecalContainerGeometry.CreateInfo(decal));
            decal.RemoveSelf();
        }

        public void Render(OriginalLevelSceneRoot level, ISourceRenderContext context, int gameplayWidth,
            int gameplayHeight)
        {
            var camera = level.Camera.Position;
            var cameraOffset = camera + new Vector2(160f, 90f);
            bool paused = level.Paused;
            foreach (var info in Decals)
            {
                var decal = info.Decal;
                if (decal.Scene == null) DecalContainerSceneBinding.SetScene(decal, level);
                var position = decal.Position;
                decal.Position = position + (position - cameraOffset) * decal.ParallaxAmount;
                bool visible = DecalContainerGeometry.IsInside(camera, info, gameplayWidth, gameplayHeight);
                decal.Position = position;
                if (!visible) continue;
                // Upstream parallax path intentionally does not test Entity.Visible.
                if (!paused && decal.Active) decal.Update();
                global::Celeste.Scene.Diagnostics.Presentation.SceneEntityRenderObservation.Render(decal, context);
            }
        }
    }
}
