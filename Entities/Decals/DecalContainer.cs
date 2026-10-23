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
    public sealed class DecalContainer
    {
        internal DecalContainerRenderer Renderer;
        internal readonly DecalInfoBuffer Decals = new();
        internal readonly HitboxColliderGeometry Collider;
        internal int MaxWidth, MaxHeight;
        public Vector2 Position;
        private bool hasSetScene;

        public DecalContainer(HitboxColliderGeometry collider) => Collider = collider;
        public bool IsDecalValid(SourceDecalEntityState decal) => Collider.Collide(decal.Position);

        public void AddDecal(SourceDecalEntityState decal)
        {
            hasSetScene = false;
            var info = DecalContainerGeometry.CreateInfo(decal);
            Decals.Add(info);
            MaxWidth = (int)Math.Max(MaxWidth, info.HalfWidth * 2);
            MaxHeight = (int)Math.Max(MaxHeight, info.HalfHeight * 2);
            foreach (var component in decal)
            {
                switch (component)
                {
                    case VertexLightState light:
                        Renderer.Add(new VertexLightState(decal.EngineContext, light.Position + decal.Position,
                            light.Color, light.Alpha, (int)light.StartRadius, (int)light.EndRadius));
                        break;
                    case BloomPointState bloom:
                        Renderer.Add(new BloomPointState(bloom.Position + decal.Position, bloom.Alpha, bloom.Radius));
                        break;
                    case SourceStaticMoverState mover:
                        mover.OnDestroy += () => Decals.Remove(info);
                        break;
                    // The upstream LightOcclude/default component transfer is explicitly not implemented.
                }
            }

            decal.RemoveSelf();
        }

        public void Awake(object scene)
        {
            Collider.Width += MaxWidth;
            Collider.Height += MaxHeight;
            Position -= new Vector2(MaxWidth / 2, MaxHeight / 2);
        }

        public void Render(OriginalLevelSceneRoot level, ISourceRenderContext context, int gameplayWidth,
            int gameplayHeight)
        {
            var camera = level.Camera.Position;
            if (!DecalContainerGeometry.ContainerInside(Position, Collider.Width, Collider.Height, camera,
                    gameplayWidth, gameplayHeight)) return;
            if (!hasSetScene)
            {
                hasSetScene = true;
                foreach (var info in Decals.AsSpan())
                    if (info.Decal.Scene == null)
                        DecalContainerSceneBinding.SetScene(info.Decal, level);
            }

            bool paused = level.Paused;
            foreach (var info in Decals.AsSpan())
            {
                var decal = info.Decal;
                if (!decal.Visible ||
                    !DecalContainerGeometry.IsInside(camera, info, gameplayWidth, gameplayHeight)) continue;
                if (!paused && decal.Active) decal.Update();
                global::Celeste.Scene.Diagnostics.Presentation.SceneEntityRenderObservation.Render(decal, context);
            }
        }
    }
}
