using System;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Components.Animation;
using Celeste.Core.Mathematics.Easing;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Scene.Entities;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Triggers.Foundation;
using Celeste.Simulation.Presentation.Backdrops.Rendering;
using Celeste.Simulation.Presentation.Backdrops.Runtime;
using Celeste.Mods.FrostHelper.Animation.Easing;
using Celeste.Mods.FrostHelper.Entities.Controllers.Stylegrounds;
using Celeste.Mods.FrostHelper.Runtime.Stylegrounds;

namespace Celeste.Mods.FrostHelper.Triggers.Stylegrounds
{
    [CustomEntity("FrostHelper/StylegroundMoveTrigger")]
    [RuntimeTypeAlias("FrostHelper.StylegroundMoveTrigger")]
    public sealed class StylegroundMoveTrigger : SourceTriggerEntityState
    {
        private readonly ISourceEngineContext engine;
        private readonly StylegroundPositions positions;
        public string TargetTag;
        public Vector2 Movement;
        public Func<float, float> Easer;
        public float Duration;
        public bool Once;
        public AfterDeathBehaviour AfterDeath;

        public StylegroundMoveTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration,
                data.Position + offset, data.Width, data.Height, ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            this.engine = engine;
            positions = StylegroundPositions.For(engine);
            TargetTag = data.Attr("tag", "");
            Easer = new FrostEasingResolver(((ISourceEasingEnvironment)engine).EasingArithmetic).Resolve(
                data.Attr("easing", "CubeInOut"));
            Duration = data.Float("duration", 1f);
            Movement = new Vector2(data.Float("moveByX", 0f), data.Float("moveByY", 0f));
            Once = data.Bool("once", false);
            AfterDeath = data.Enum("afterDeath", AfterDeathBehaviour.Reset);
        }

        public override void OnEnter(RoomPlayerSession player)
        {
            base.OnEnter(player);
            var level = (OriginalLevelSceneRoot)engine.Frame.Scenes.CurrentScene;
            SceneEntityState holder = Once ? StylegroundTweenHolder.Ensure(engine, level) : this;
            HandleRenderer(level.Background, holder);
            HandleRenderer(level.Foreground, holder);
            if (Once) RemoveSelf();
        }

        private void StoreOriginal(BackdropPresentation backdrop)
        {
            if (AfterDeath == AfterDeathBehaviour.Reset) positions.StoreOriginal(backdrop);
        }

        private void HandleRenderer(BackdropRendererPresentation renderer, SceneEntityState holder)
        {
            float duration = Duration;
            if (duration == 0f)
            {
                foreach (var backdrop in renderer.Backdrops)
                    if (backdrop.Tags.Contains(TargetTag))
                    {
                        StoreOriginal(backdrop);
                        backdrop.Position += Movement;
                    }

                return;
            }

            foreach (var backdrop in renderer.Backdrops)
            {
                if (!backdrop.Tags.Contains(TargetTag)) continue;
                var tween = SourceTweenState.Create(engine.TweenPool, SourceTweenState.TweenMode.Oneshot,
                    Easer, duration, true);
                holder.OwnedComponents.Add(tween);
                Vector2 startPosition = backdrop.Position;
                Vector2 lastMovement = Vector2.Zero;
                if (AfterDeath == AfterDeathBehaviour.SnapToEnd)
                    positions.StoreEnd(backdrop, backdrop.Position + Movement);
                else StoreOriginal(backdrop);
                tween.OnUpdate = current =>
                {
                    float eased = current.Easer(Math.Min(1f, current.Percent));
                    Vector2 movement = Movement * eased;
                    backdrop.Position += movement - lastMovement;
                    lastMovement = movement;
                };
                tween.OnComplete = current => backdrop.Position = startPosition + Movement;
            }
        }
    }
}
