using System;
using System.Globalization;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Scheduling;
using Celeste.Core.Audio;
using Celeste.Core.Reflection.Runtime;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Enemies.Badeline.Chasers;
using Celeste.Game.Gameplay.Enemies.Badeline.Chasers;
using Celeste.Mods.FrostHelper.Entities.Platforms.BadelineChasers;
using Celeste.Mods.FrostHelper.Components.Lifecycle;
using Celeste.Mods.FrostHelper.Runtime.BadelineChasers;
using Celeste.Physics.Collision;

namespace Celeste.Mods.FrostHelper.Entities.Controllers.BadelineChasers
{
    [RuntimeTypeAlias("FrostHelper.BadelineChaserBlockManager")]
    public class BadelineChaserBlockManager : SceneEntityState
    {
        private readonly ISourceEngineContext engine;
        private bool lastState;
        private char[] animationBuffer = new char[32];

        public BadelineChaserBlockManager(ISourceEngineContext engine)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, Vector2.Zero)
        {
            this.engine = engine;
            Depth = -1000000;
        }

        internal static BadelineChaserBlockManager Ensure(ISourceEngineContext engine, ISourceScene scene,
            BadelineChaserBlockResources resources)
        {
            if (scene.SourceScene.Entities.TryGetTrackedEntities(typeof(BadelineChaserBlockManager), out var managers)
                && managers.Count != 0) return (BadelineChaserBlockManager)managers[0];
            if (resources.PendingManager == null)
            {
                resources.PendingManager = new BadelineChaserBlockManager(engine);
                scene.SourceScene.AddEntity(resources.PendingManager);
                resources.PendingManager.Add(new OnAwakeCallbackComponent(() => resources.PendingManager = null));
            }

            return resources.PendingManager;
        }

        private bool IsHoldingWall(string animation)
        {
            if (animation.Length > animationBuffer.Length) animationBuffer = new char[animation.Length];
            int length = animation.AsSpan().ToLower(animationBuffer.AsSpan(), CultureInfo.CurrentCulture);
            var lowered = animationBuffer.AsSpan(0, length);
            return lowered.IndexOf("climb".AsSpan()) >= 0 || lowered.SequenceEqual("dangling".AsSpan())
                                                          || lowered.SequenceEqual("wallslide".AsSpan());
        }

        public override void Update()
        {
            var scene = ((ISourceScene)Scene).SourceScene;
            var entities = scene.Entities;
            bool state = false;
            var player = entities.GetTrackedEntity<RoomPlayerSession>();
            foreach (BadelineChaserBlockActivator activator in entities
                         .GetEntitiesTrackIfNeeded<BadelineChaserBlockActivator>())
            {
                activator.DoneCollisionChecks = true;
                if (state) continue;
                if (activator.Solid && activator.HasBaddyRider())
                {
                    state = true;
                    break;
                }

                foreach (BadelineOldsiteState chaser in entities.GetEntitiesTrackIfNeeded<BadelineOldsiteState>())
                {
                    if (activator.Solid)
                    {
                        if ((SourceEntityCollision.CheckAt(chaser, activator, chaser.Position + Vector2.UnitX * 2f)
                             || SourceEntityCollision.CheckAt(chaser, activator, chaser.Position + Vector2.UnitX * -2f))
                            && player != null && !player.Dead && BadelineChaserFields.Following(chaser)
                            && player.GetChasePosition(scene.Clock.TimeActive,
                                BadelineChaserFields.FollowBehindTime(chaser) +
                                BadelineChaserFields.FollowBehindIndexDelay(chaser), out var chase))
                        {
                            if (IsHoldingWall(chase.Animation))
                            {
                                state = true;
                                break;
                            }
                        }
                    }
                    else
                    {
                        activator.Collidable = true;
                        if (SourceEntityCollision.Check(chaser, activator))
                        {
                            state = true;
                            activator.Collidable = false;
                            break;
                        }

                        activator.Collidable = false;
                    }
                }
            }

            foreach (BadelineChaserBlock block in entities.GetEntitiesTrackIfNeeded<BadelineChaserBlock>())
                block.SetState(state);
            if (lastState != state)
            {
                var sound = ((ISourceAudioEnvironment)engine).SourceAudio.Play(state
                    ? BadelineChaserBlockActivator.ActivateSfx
                    : BadelineChaserBlockActivator.DeactivateSfx);
                sound.GetVolume(out var volume, out _);
                sound.SetVolume(volume * 60f);
            }

            lastState = state;
        }
    }
}
