// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.FrostHelper.Filtering.Entities;
using Celeste.Mods.FrostHelper.Rendering.Batches;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation;

namespace Celeste.Mods.FrostHelper.Entities.Rendering
{
    [CustomEntity("FrostHelper/EntityBatcher")]
    [RuntimeTypeAlias("FrostHelper.EntityBatcher")]
    public sealed class EntityBatcher : SceneEntityState
    {
        public Type[] Types;
        public string Shader;
        public List<SceneEntityState> AffectedEntities;
        public Dictionary<string, object> ShaderParameters;
        public bool MakeEntitiesInvisible;
        public int[] DynamicDepthPossibleDepths;
        public string Flag;
        public bool FlagInverted;
        private object _firstScene;
        public bool ConsumeStylegrounds;
        private readonly ISourceEngineContext engine;
        private readonly EntityBatchRenderProcess renderer;

        public static EntityBatcher Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) => new(level.EngineContext, data, offset);

        public EntityBatcher(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
            this.engine = engine;
            renderer = EntityBatchRenderProcess.For(engine);
            Flag = data.Attr("flag", string.Empty);
            FlagInverted = data.Bool("flagInverted", false);
            Shader = data.Attr("effect", "");
            Depth = data.Int("depth", -1000000);
            string types = data.Attr("types", "");
            if (types == string.Empty) Types = Type.EmptyTypes;
            else
            {
                var resolver = new FrostEntityTypeResolver(engine);
                Types = types.Trim().Split(',').Select(name => resolver.Resolve(name.Trim())).ToArray();
            }

            MakeEntitiesInvisible = data.Bool("makeEntitiesInvisible", true);
            ConsumeStylegrounds = data.Bool("consumeStylegrounds", false);
            Visible = true;
            ShaderParameters = new();
            foreach (string part in data.Attr("parameters", string.Empty)
                         .Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int separator = part.IndexOf(':');
                ShaderParameters[part.Substring(0, separator)] = part.Substring(separator + 1);
            }

            string depths = data.Attr("dynamicDepthBatchSplitField", string.Empty);
            if (depths != string.Empty) DynamicDepthPossibleDepths = depths.Split(',').Select(int.Parse).ToArray();
            Add(new RoomTransitionListener { OnOut = _ => Visible = false });
        }

        public EntityBatcher(EntityBatcher from, int targetDepth)
            : base(((ISourceEntitySourceDataEnvironment)from.engine).EntitySourceDataRegistration)
        {
            engine = from.engine;
            renderer = from.renderer;
            Shader = from.Shader;
            ShaderParameters = from.ShaderParameters;
            Types = from.Types;
            Flag = from.Flag;
            FlagInverted = from.FlagInverted;
            Visible = true;
            Depth = targetDepth;
            DynamicDepthPossibleDepths = new[] { targetDepth };
        }

        public bool IsEnabled() =>
            Flag == string.Empty || ((OriginalLevelSceneRoot)Scene).Session.GetFlag(Flag) ^ FlagInverted;

        public override void Awake(object scene)
        {
            base.Awake(scene);
            _firstScene ??= scene;
            if (AffectedEntities != null) return;
            AffectedEntities = new();
            if (DynamicDepthPossibleDepths != null && DynamicDepthPossibleDepths.Length > 1)
            {
                foreach (int depth in DynamicDepthPossibleDepths)
                {
                    var child = new EntityBatcher(this, depth) { Depth = depth };
                    ((Celeste.Scene.Scenes.SourceSceneState)scene).AddEntity(child);
                }

                RemoveSelf();
                return;
            }

            foreach (object value in ((ISourceScene)scene).SourceScene.Entities)
                if (Types.Contains(value.GetType()))
                {
                    var entity = (SceneEntityState)value;
                    AffectedEntities.Add(entity);
                    if (MakeEntitiesInvisible) entity.Visible = false;
                }
        }

        public override void Update()
        {
            base.Update();
            Visible = IsEnabled();
            if (!MakeEntitiesInvisible) return;
            foreach (var entity in AffectedEntities) entity.Visible = !Visible;
        }

        public override void Render(ISourceRenderContext context)
        {
            if (ReferenceEquals(engine.Frame.Scenes.CurrentScene, _firstScene))
                renderer.Apply(context, AffectedEntities, Shader, ShaderParameters,
                    DynamicDepthPossibleDepths != null ? Depth : null, ConsumeStylegrounds, MakeEntitiesInvisible);
        }

        internal static void RenderCapturedEntities(ISourceRenderContext context, List<SceneEntityState> entities,
            int? requiredDepth, bool makeEntitiesInvisible)
        {
            foreach (var entity in entities)
                if (entity != null && entity.Scene != null && (!requiredDepth.HasValue || entity.Depth == requiredDepth)
                    && (makeEntitiesInvisible || entity.Visible))
                {
                    bool visible = entity.Visible;
                    entity.Visible = true;
                    global::Celeste.Scene.Diagnostics.Presentation.SceneEntityRenderObservation.Render(entity, context);
                    entity.Visible = visible;
                }
        }
    }
}
