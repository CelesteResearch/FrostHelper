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
using Celeste.Core.Mathematics.Extrema;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Simulation.Gameplay.Triggers.Foundation;
using Celeste.Simulation.Presentation.Effects.Mirrors;
using Celeste.Mods.FrostHelper.Runtime.Decals;

namespace Celeste.Mods.FrostHelper.Entities.Decals
{
    [CustomEntity("FrostHelper/DecalContainer")]
    [Celeste.Core.Reflection.Runtime.RuntimeTypeAlias("FrostHelper.DecalContainerMaker")]
    public sealed class DecalContainerMaker : SourceTriggerEntityState, ISourceEngineContextEntity
    {
        private readonly ISourceEngineContext engine;
        public readonly int ChunkSize;
        ISourceEngineContext ISourceEngineContextEntity.EngineContext => engine;

        public DecalContainerMaker(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset,
                data.Width, data.Height, ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            this.engine = engine;
            ChunkSize = data.Int("chunkSizeInTiles", 64) * 8;
            Depth = -1000000;
        }

        public override void Awake(object scene)
        {
            var renderers = new Dictionary<int, DecalContainerRenderer>();
            var bounds = ((OriginalLevelSceneRoot)Scene).Bounds;
            var entities = ((ISourceScene)scene).SourceScene.Entities;
            foreach (var item in entities)
            {
                if (item is not SourceDecalEntityState decal || renderers.ContainsKey(decal.Depth)) continue;
                var renderer = new DecalContainerRenderer(engine) { Depth = decal.Depth };
                renderers[decal.Depth] = renderer;
                ((ISourceScene)scene).SourceScene.AddEntity(renderer);
                for (int x = bounds.Left; x < bounds.Right; x += ChunkSize)
                for (int y = bounds.Top; y < bounds.Bottom; y += ChunkSize)
                    renderer.Containers.Add(new DecalContainer(new HitboxColliderGeometry(ChunkSize, ChunkSize, x, y))
                        { Position = new Vector2(x, y), Renderer = renderer });
            }

            DecalContainer lastContainer = null;
            var ignored = FrostDecalContainerState.For(engine).IgnoredNames;
            foreach (var entity in entities)
            {
                if (entity is not SourceDecalEntityState decal || ignored.Contains(decal.Name) ||
                    decal.Get<MirrorSurfaceState>() != null) continue;
                var renderer = renderers[decal.Depth];
                if (decal.Parallax)
                {
                    renderer.Parallax.AddDecal(decal);
                    continue;
                }

                DecalContainer newContainer = null;
                for (int index = renderer.Containers.Count - 1; index >= 0; index--)
                {
                    var container = renderer.Containers[index];
                    if (!container.IsDecalValid(decal)) continue;
                    if (lastContainer == null || ReferenceEquals(lastContainer, container))
                    {
                        lastContainer = container;
                        container.AddDecal(decal);
                    }
                    else
                    {
                        newContainer = new DecalContainer(container.Collider)
                            { Position = container.Position, Renderer = container.Renderer };
                        newContainer.AddDecal(decal);
                        lastContainer = newContainer;
                    }

                    break;
                }

                if (newContainer != null) renderer.Containers.Add(newContainer);
            }

            RemoveSelf();
        }
    }
}
