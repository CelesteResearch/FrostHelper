// SPDX-License-Identifier: MIT

using System;
using System.Collections;
using System.Numerics;
using Celeste.Core.Mathematics;
using Celeste.Core.Components;
using Celeste.Core.Components.Timing;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Startup;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Mods.FrostHelper.Collider.Spinners;
using Celeste.Mods.FrostHelper.Runtime.Boosters;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Scene.Components.Oscillation;
using Celeste.Scene.Scheduling.ExecutionTags;
using Celeste.Simulation;
using Celeste.Simulation.Audio.Sources;
using Celeste.Simulation.Gameplay.Player.Collision.Components;
using Celeste.Simulation.Gameplay.Player.Dash;
using Celeste.Simulation.Gameplay.Player.Scene;
using Celeste.Simulation.Presentation.Effects.Mirrors;
using Celeste.Simulation.Presentation.Lighting;
using Celeste.Simulation.Presentation.PostProcessing.Bloom;
using Celeste.Simulation.Presentation.Particles;
using Celeste.Simulation.Presentation.Particles.Definitions;
using Celeste.Core.Display;

namespace Celeste.Mods.FrostHelper.Entities.Boosters
{
    [SceneTracked(true)]
    [global::Celeste.Core.Reflection.Runtime.RuntimeTypeAlias("FrostHelper.Entities.Boosters.GenericCustomBooster")]
    public class GenericCustomBooster : SceneEntityState
    {
        public static string DynamicDataName => "fh.booster";
        private readonly FrostBoosterRuntime runtime;
        private ISourceVectorArithmetic Arithmetic => ((ISourceVectorEnvironment)runtime.Engine).VectorArithmetic;
        private ISourceGameGraphicsResources Graphics => ((ISourceGameGraphicsEnvironment)runtime.Engine).GameGraphics;
        private IOriginalGameplayProcess Process => (IOriginalGameplayProcess)runtime.Engine;

        private void Play(string name, Vector2 position) =>
            Process.AudioSession.Play(name, position, Process.SaveContext);

        private static string Attr(EntityData data, string key, string fallback) =>
            EntityAttributeReader.Attr(data.Values, key, fallback);

        private RoomPlayerSession NearestPlayer()
        {
            var list = ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntities(typeof(RoomPlayerSession));
            RoomPlayerSession nearest = null;
            float distance = float.MaxValue;
            foreach (RoomPlayerSession player in list)
            {
                float current = Arithmetic.LengthSquared(player.Position - Position);
                if (current < distance)
                {
                    distance = current;
                    nearest = player;
                }
            }

            return nearest;
        }

        public bool BoostingPlayer { get; private set; }
        public string reappearSfx;
        public string enterSfx;
        public string boostSfx;
        public string endSfx;
        public float BoostTime;
        public PackedColor ParticleColor;
        public PackedColor OutlineColor;
        public bool Red;
        public float RespawnTime;
        public bool PreserveSpeed;

        /// <summary>
        /// Set to -1 to refill dashes to dash cap (default)
        /// Set to -2 to not touch dash count at all
        /// </summary>
        public int DashRecovery;

        public bool StaminaRecovery;

        /// <summary>
        /// Speed at which the player entered the booster
        /// </summary>
        public Vector2 EnterSpeed;

        // respawnTime = 0 would already make the booster not respawn in earlier versions, so it will stay that way, even if it could be unintuitive
        public bool ShouldRespawn => RespawnTime > 0f;

        public readonly RedBoostDashOutModes RedBoostDashOutMode;

        public readonly string Directory;

        public GenericCustomBooster(FrostBoosterRuntime runtime, EntityData data, Vector2 offset) : base(
            ((ISourceEntitySourceDataEnvironment)runtime.Engine).EntitySourceDataRegistration, data.Position + offset)
        {
            this.runtime = runtime;

            Depth = -8500;
            Collider = SpinnerHitboxReader.Create(Attr(data, "hitbox", ""), 1f, Arithmetic, runtime.Warn) ??
                       new CircleColliderGeometry(Arithmetic, 10f, 0f, 2f);
            Directory = Attr(data, "directory", "objects/FrostHelper/blueBooster/");
            sprite = new Sprite(runtime.Engine, Graphics.GameAtlas, Directory)
            {
                Visible = true
            };
            sprite.CenterOrigin();
            sprite.Justify = new Vector2(0.5f, 0.5f);
            sprite.AddLoop("loop", "booster", 0.1f, 0, 1, 2, 3, 4);
            sprite.AddLoop("inside", "booster", 0.1f, 5, 6, 7, 8);
            sprite.AddLoop("spin", "booster", 0.06f, 18, 19, 20, 21, 22, 23, 24, 25);
            sprite.Add("pop", "booster", 0.08f, 9, 10, 11, 12, 13, 14, 15, 16, 17);
            sprite.Play("loop", false);
            OwnedComponents.Add(sprite);

            OwnedComponents.Add(new PlayerColliderComponentState(OnPlayer));
            OwnedComponents.Add(light = new VertexLightState(runtime.Engine, PackedColor.White, 1f, 16, 32));
            OwnedComponents.Add(bloom = new BloomPointState(0.1f, 16f));
            OwnedComponents.Add(wiggler =
                ((ISourceWigglerPoolScope)runtime.Engine).WigglerPool.Create(0.5f, 4f,
                    f => { sprite.Scale = Vector2.One * (1f + f * 0.25f); }));
            OwnedComponents.Add(dashRoutine = new SimulationCoroutine(runtime.Engine, removeOnComplete: false));

            OwnedComponents.Add(dashListener = new DashListenerState());
            dashListener.OnDash = OnPlayerDashed;

            OwnedComponents.Add(new MirrorReflectionState());
            OwnedComponents.Add(loopingSfx = ((ISourceSoundSourceEnvironment)runtime.Engine).SoundSources.Create());

            Red = EntityAttributeReader.Bool(data.Values, "red", false);
            particleType = Red ? Process.ParticleTypes.Booster.P_BurstRed : Process.ParticleTypes.Booster.P_Burst;

            RespawnTime = EntityAttributeReader.Float(data.Values, "respawnTime", 1f);
            BoostTime = EntityAttributeReader.Float(data.Values, "boostTime", 0.3f);
            ParticleColor = runtime.Colors.Read(Attr(data, "particleColor", "Yellow"));
            reappearSfx = Attr(data, "reappearSfx", "event:/game/04_cliffside/greenbooster_reappear");
            enterSfx = Attr(data, "enterSfx", "event:/game/04_cliffside/greenbooster_enter");
            boostSfx = Attr(data, "boostSfx", "event:/game/04_cliffside/greenbooster_dash");
            endSfx = Attr(data, "releaseSfx", "event:/game/04_cliffside/greenbooster_end");
            DashRecovery = EntityAttributeReader.Int(data.Values, "dashes", -1);
            PreserveSpeed = EntityAttributeReader.Bool(data.Values, "preserveSpeed", false);
            OutlineColor = runtime.Colors.Read(Attr(data, "outlineColor", "000000"));
            RedBoostDashOutMode =
                Enum.TryParse<RedBoostDashOutModes>(Attr(data, "redBoostDashOutMode", "Default"), true, out var mode)
                    ? mode
                    : RedBoostDashOutModes.Default;
            StaminaRecovery = EntityAttributeReader.Bool(data.Values, "staminaRecovery", true);
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            string outlinePath = $"{Directory.AsSpan().TrimEnd('/')}/outline";
            SpriteImage image = new SpriteImage(Graphics.GameAtlas.Entries.TryGetValue(outlinePath, out var texture)
                ? texture
                : Graphics.GameAtlas.Entries["objects/booster/outline"]);
            image.CenterOrigin();
            image.Color = PackedColor.White * 0.75f;
            outline = new SceneEntityState(
                ((ISourceEntitySourceDataEnvironment)runtime.Engine).EntitySourceDataRegistration, Position)
            {
                Depth = 8999,
                Visible = false
            };
            outline.OwnedComponents.Add(image);
            outline.OwnedComponents.Add(new MirrorReflectionState());
            ((ISourceScene)scene).SourceScene.QueueSourceEntity(outline);
        }

        public void Appear()
        {
            Play(reappearSfx, Position);
            sprite.Play("appear", false, false);
            wiggler.Start();
            Visible = true;
            AppearParticles();
        }

        private void AppearParticles()
        {
            ParticleSystemPresentation particlesBG = ((OriginalLevelSceneRoot)Scene).ParticlesBG;
            for (int i = 0; i < 360; i += 30)
            {
                particlesBG.Emit(
                    Red ? Process.ParticleTypes.Booster.P_RedAppear : Process.ParticleTypes.Booster.P_Appear,
                    1, Center, Vector2.One * 2f, ParticleColor, i * 0.0174532924f);
            }
        }

        public virtual void OnPlayer(RoomPlayerSession player)
        {
            if (respawnTimer <= 0f && cannotUseTimer <= 0f && !BoostingPlayer)
            {
                cannotUseTimer = 0.45f - 0.25f + BoostTime;

                Boost(player);

                Play(enterSfx, Position);
                wiggler.Start();
                sprite.Play("inside", false, false);
                sprite.FlipX = player.Facing == -1;
            }
        }

        public bool StartedBoosting;

        public virtual void Boost(RoomPlayerSession player)
        {
            runtime.ForPlayer(player).Boost(this);
        }

        public virtual float GetBoostSpeed() =>
            PreserveSpeed ? Math.Max(Math.Abs(EnterSpeed.X), Math.Abs(EnterSpeed.Y)) : 240f;


        public virtual bool CanFastbubble() => true;

        public void PlayerBoosted(RoomPlayerSession player, Vector2 direction)
        {
            if (Red)
            {
                loopingSfx.Play("event:/game/05_mirror_temple/redbooster_move");
                loopingSfx.DisposeOnTransition = false;
            }

            StartedBoosting = false;
            Play(boostSfx, Position);
            BoostingPlayer = true;
            Tags = ((ISourcePersistentTransitionExecutionTagsEnvironment)runtime.Engine)
                .PersistentTransitionExecutionTags
                .Persistent.Value | ((ISourcePersistentTransitionExecutionTagsEnvironment)runtime.Engine)
                .PersistentTransitionExecutionTags.TransitionUpdate.Value;
            sprite.Play("spin", false, false);
            sprite.FlipX = player.Facing == -1;

            if (ShouldRespawn)
                outline.Visible = true;
            wiggler.Start();
            dashRoutine.Replace(BoostRoutine(player, direction));
        }

        private IEnumerator BoostRoutine(RoomPlayerSession player, Vector2 dir)
        {
            var state = runtime.ForPlayer(player);
            float angle = (float)Math.Atan2(-dir.Y, -dir.X);
            while ((player.Machine.State == 2 || player.Machine.State == state.RedState) && BoostingPlayer)
            {
                if (player.Dead)
                {
                    PlayerDied();
                }
                else
                {
                    sprite.RenderPosition = player.Center + PlayerOffset;
                    loopingSfx.Position = sprite.Position;
                    if (((ISourceScene)Scene).SourceScene.Clock.OnInterval(0.02f, runtime.Engine))
                    {
                        (Scene as OriginalLevelSceneRoot)!.ParticlesBG.Emit(particleType, 2,
                            player.Center - dir * 3f + new Vector2(0f, -2f), new Vector2(3f, 3f), ParticleColor, angle);
                    }

                    yield return null;
                }
            }

            PlayerReleased();

            if (state.IsInBoostState)
            {
                sprite.Visible = false;
            }

            while (((OriginalLevelSceneRoot)Scene).Transitioning)
            {
                yield return null;
            }

            Tags = 0;
            yield break;
        }

        public virtual void OnPlayerDashed(Vector2 direction)
        {
            if (BoostingPlayer)
            {
                BoostingPlayer = false;
            }
        }

        public virtual void PlayerReleased()
        {
            Play(endSfx, sprite.RenderPosition);
            sprite.Play("pop", false, false);

            if (!ShouldRespawn)
            {
                sprite.OnLastFrame += (s) => { RemoveSelf(); };
                Collidable = false;
                bloom.Visible = false;
                light.Visible = false;
            }

            respawnTimer = RespawnTime;

            cannotUseTimer = 0f;

            BoostingPlayer = false;
            wiggler.Stop();
            loopingSfx.Stop(true);
        }

        public virtual void PlayerDied()
        {
            if (BoostingPlayer)
            {
                PlayerReleased();
                dashRoutine.Active = false;
                Tags = 0;
            }
        }

        public virtual void Respawn()
        {
            Play(reappearSfx, Position);
            sprite.Position = Vector2.Zero;
            sprite.Play("loop", true, false);
            wiggler.Start();
            sprite.Visible = true;
            outline.Visible = false;
            AppearParticles();
        }

        public override void Update()
        {
            base.Update();

            if (cannotUseTimer > 0f)
            {
                cannotUseTimer -= runtime.Engine.DeltaTime;
            }

            if (ShouldRespawn && respawnTimer > 0f)
            {
                respawnTimer -= runtime.Engine.DeltaTime;
                if (respawnTimer <= 0f)
                {
                    Respawn();
                }
            }

            RoomPlayerSession player = NearestPlayer();

            if (!dashRoutine.Active && respawnTimer <= 0f && Collidable)
            {
                Vector2 target = Vector2.Zero;

                if (player != null && SourceEntityCollision.Check(this, player))
                {
                    target = player.Center + PlayerOffset - Position;
                }

                sprite.Position = Arithmetic.Approach(sprite.Position, target, 80f * runtime.Engine.DeltaTime);
            }

            //if (player != null && GetBoosterThatIsBoostingPlayer(player) == this && BoostTime > 0f) {
            //sprite.Position = player.Center + Booster.playerOffset - Position;

            // if the player is far away, render the outline because clearly the bubble got moved
            //if (Vector2.DistanceSquared(Position, player.Center) > 16f * 16f)
            //    outline.Visible = true;
            //}

            if (sprite.CurrentAnimationID == "inside" && !BoostingPlayer &&
                (player is null || !SourceEntityCollision.Check(this, player)))
            {
                sprite.Play("loop", false, false);
            }
        }

        public override void Render(ISourceRenderContext context)
        {
            Vector2 position = sprite.Position;
            sprite.Position = new Vector2((float)Math.Floor(position.X), (float)Math.Floor(position.Y));
            var lastOutlineVisible = outline.Visible;

            if (sprite.Visible && sprite.CurrentAnimationID != "pop")
            {
                if (OutlineColor.A > 0)
                    sprite.DrawOutline(context, OutlineColor, 1);
                outline.Visible = false;
            }

            base.Render(context);
            sprite.Position = position;
            outline.Visible = lastOutlineVisible;
        }

        public virtual void HandleDashRefill(RoomPlayerSession player)
        {
            switch (DashRecovery)
            {
                case -1:
                    player.RefillDash();
                    break;
                case -2:
                    // no recovery
                    break;
                case < -2:
                    runtime.Warn(
                        $"Invalid value for the 'dashes' property: {DashRecovery}.\nExpected a value >= -2.\nReport this to the mapmaker!");
                    break;
                default:
                    player.Data.Dashes = DashRecovery;
                    break;
            }
        }

        public virtual void HandleBoostBegin(RoomPlayerSession player)
        {
            bool doNotDropTheo = (player.Scene as ISourcePlayerBoostMetadataScene)?.BoostTheoInBubble ?? false;
            HandleDashRefill(player);

            if (StaminaRecovery)
                player.RefillStamina();
            if (doNotDropTheo)
            {
                return;
            }

            player.Drop();
        }

        public virtual IEnumerator HandleBoostCoroutine(RoomPlayerSession player)
        {
            if (BoostTime > 0.25f)
            {
                yield return BoostTime - 0.25f;
                Play(boostSfx, Position);
                Flash();
                yield return 0.25f;
            }
            else
            {
                yield return BoostTime;
            }

            player.Machine.State = runtime.ForPlayer(player).ExitBoostState(this);
            yield break;
        }

        public virtual void OnBoostEnd(RoomPlayerSession player)
        {
        }

        public virtual void Flash()
        {
        }

        public ParticleTypePresentation P_Burst => Process.ParticleTypes.Booster.P_Burst;

        public ParticleTypePresentation P_BurstRed => Process.ParticleTypes.Booster.P_BurstRed;

        public ParticleTypePresentation P_Appear => Process.ParticleTypes.Booster.P_Appear;

        public ParticleTypePresentation P_RedAppear => Process.ParticleTypes.Booster.P_RedAppear;

        public Vector2 PlayerOffset => runtime.PlayerOffset;

        public Sprite sprite;

        public SceneEntityState outline;

        public WigglerComponent wiggler;

        public BloomPointState bloom;

        public VertexLightState light;

        public SimulationCoroutine dashRoutine;

        public DashListenerState dashListener;

        public ParticleTypePresentation particleType;

        public float respawnTimer;

        public float cannotUseTimer;

        public SoundSourceComponentState loopingSfx;
    }
}
