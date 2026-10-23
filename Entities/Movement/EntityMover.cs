// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Components.Animation;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Easing;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Mods.FrostHelper.Animation.Easing;
using Celeste.Mods.FrostHelper.Filtering.Entities;
using Celeste.Mods.FrostHelper.Movement.Entities;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;

namespace Celeste.Mods.FrostHelper.Entities.Movement
{
    [CustomEntity("FrostHelper/EntityMover")]
    [RuntimeTypeAlias("FrostHelper.EntityMover")]
    public sealed class EntityMover : SceneEntityState
    {
        private readonly ISourceEngineContext engine;
        private readonly FrostEntityFilter filter;
        private readonly System.Func<float, float> easing;
        private readonly List<(SceneEntityState Entity, Vector2 Start)> entities = new();
        private readonly Vector2 endNode;
        private readonly Vector2 distance;
        private readonly float duration;
        private readonly float pauseTime;
        private readonly bool mustCollide;
        private readonly bool relativeMode;
        private readonly string onEndSound;
        private float pauseTimer;
        private SourceTweenState tween;
        private bool moveBack;

        public static EntityMover Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) => new(data, offset, level.EngineContext);

        public EntityMover(EntityData data, Vector2 offset, ISourceEngineContext engine)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
            this.engine = engine;
            Collider = new HitboxColliderGeometry(data.Width, data.Height);
            filter = FrostEntityFilter.CreateFrom(data, new FrostEntityTypeResolver(engine));
            pauseTime = data.Float("pauseTimeLength");
            pauseTimer = data.Float("startPauseTimeLength");
            relativeMode = data.Bool("relativeMovementMode");
            onEndSound = data.Attr("onEndSFX");
            endNode = data.FirstNodeNullable(offset).GetValueOrDefault();
            easing = new FrostEasingResolver(((ISourceEasingEnvironment)engine).EasingArithmetic).Resolve(
                data.Attr("easing", "CubeInOut"));
            duration = data.Float("moveDuration", 1f);
            mustCollide = data.Bool("mustCollide", true);
            distance = endNode - Position;
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            foreach (object entry in ((ISourceScene)scene).SourceScene.Entities.Entities)
                if (entry is SceneEntityState entity && (!mustCollide || Collider.Collide(entity.Position)) &&
                    filter.Matches(entity))
                    entities.Add((entity, entity.Position));
            tween = SourceTweenState.Create(engine.TweenPool, SourceTweenState.TweenMode.Looping, easing, duration,
                true);
            tween.OnUpdate = current =>
            {
                var arithmetic = ((ISourceScalarEnvironment)engine).ScalarArithmetic;
                foreach (var pair in entities)
                {
                    var end = relativeMode ? pair.Start + distance : endNode;
                    var to = moveBack
                        ? Vector2Interpolation.Lerp(end, pair.Start, current.Eased, arithmetic)
                        : Vector2Interpolation.Lerp(pair.Start, end, current.Eased, arithmetic);
                    FrostEntityMovement.MoveEntity(pair.Entity, to);
                }
            };
            tween.OnComplete = _ =>
            {
                moveBack = !moveBack;
                pauseTimer = pauseTime;
                if (onEndSound != "") ((IOriginalGameplayProcess)engine).AudioSession.Play(onEndSound);
            };
            Add(tween);
        }

        public override void Update()
        {
            // Source deliberately bypasses Entity.Update; update the looping tween once after each pause.
            if (pauseTimer > 0f) pauseTimer -= engine.DeltaTime;
            else tween.Update();
        }
    }
}
