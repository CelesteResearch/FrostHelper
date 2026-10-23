using Celeste.Core.Display;
using Celeste.Formats.Map.Entities.Attributes;
using System.Runtime.CompilerServices;
using Celeste.Physics.Collision.Shapes;
using System;
using System.Numerics;
using Celeste.Core.Components.Animation;
using Celeste.Core.Mathematics;
using Celeste.Core.Randomness;
using Celeste.Scene.Entities;
using Celeste.Scene.Components.Oscillation;
using Celeste.Game.Execution.Levels;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Environment.Core;
using Celeste.Simulation.Gameplay.Player.Collision.Components;
using Celeste.Simulation.Presentation.Lighting;
using Celeste.Simulation.Presentation.PostProcessing.Bloom;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Display;

namespace Celeste.Mods.FrostHelper.Entities.Hazards.Bumpers
{
    [Celeste.Everest.Modules.Entities.Metadata.CustomEntity("FrostHelper/StaticBumper")]
    [Celeste.Core.Reflection.Runtime.RuntimeTypeAlias("FrostHelper.StaticBumper")]
    public class StaticBumper : SceneEntityState
    {
        protected readonly global::Celeste.Core.Scheduling.ISourceEngineContext engine;


        public bool Wobbling;
        public bool NotCoreMode;
        private float respawnTime = .6f;
        private float moveTime;

        private const float MoveCycleTime = 1.8181819f;

        private const float SineCycleFreq = 0.44f;

        private Sprite sprite;

        private Sprite spriteEvil;

        private VertexLightState light;

        private BloomPointState bloom;


        private bool goBack;

        private Vector2 anchor;

        private SineWaveState sine;

        private float respawnTimer;

        private bool fireMode;

        private WigglerComponent hitWiggler;

        private Vector2 hitDir;

        public StaticBumper(global::Celeste.Core.Scheduling.ISourceEngineContext engine,
            Celeste.Game.Content.Maps.Entities.OriginalLevelEntityData data, Vector2 offset)
            : base(
                ((global::Celeste.Core.Components.ISourceEntitySourceDataEnvironment)engine)
                .EntitySourceDataRegistration,
                data.Position + offset)
        {
            this.engine = engine;
            respawnTime = data.Float("respawnTime", .6f);
            string spriteKey = data.Attr("sprite", "bumper");
            Wobbling = data.Bool("wobble");
            NotCoreMode = data.Bool("notCoreMode");
            moveTime = data.Float("moveTime", MoveCycleTime);
            Collider = Celeste.Mods.FrostHelper.Collider.Spinners.SpinnerHitboxReader.Create(data.Attr("hitbox"), 1f,
                           ((Celeste.Core.Mathematics.ISourceVectorEnvironment)engine).VectorArithmetic,
                           warning => ((Celeste.Everest.Diagnostics.Logging.IEverestLoggerProcessEnvironment)engine)
                               .Logger.Log(
                                   Celeste.Everest.EverestLogLevel.Warn, "FrostHelper", warning)) ??
                       new CircleColliderGeometry(
                           ((Celeste.Core.Mathematics.ISourceVectorEnvironment)engine).VectorArithmetic, 12f);
            OwnedComponents.Add(new PlayerColliderComponentState(OnPlayer));
            OwnedComponents.Add(sine = new SineWaveState(engine, 0.44f).Randomize());
            OwnedComponents.Add(sprite = ((global::Celeste.Graphics.Content.ISourceGameGraphicsEnvironment)engine)
                .GameGraphics.SpriteFactory.Create(spriteKey));
            OwnedComponents.Add(spriteEvil = ((global::Celeste.Graphics.Content.ISourceGameGraphicsEnvironment)engine)
                .GameGraphics.SpriteFactory.Create(spriteKey + "_evil"));
            spriteEvil.Visible = false;
            OwnedComponents.Add(light =
                new VertexLightState(engine, new PackedColor(0, 128, 128, 255), 1f, 16, 32));
            OwnedComponents.Add(bloom = new BloomPointState(0.5f, 16f));
            anchor = Position;
            Vector2? node = data.FirstNodeNullable(offset);
            if (node.HasValue)
            {
                Vector2 start = Position;
                Vector2 end = node.Value;
                SourceTweenState tween = SourceTweenState.Create(engine.TweenPool, SourceTweenState.TweenMode.Looping,
                    new Celeste.Mods.FrostHelper.Animation.Easing.FrostEasingResolver(
                            ((Celeste.Core.Mathematics.Easing.ISourceEasingEnvironment)engine).EasingArithmetic)
                        .Resolve(data.Attr("easing", "CubeInOut")),
                    moveTime, start: true);
                tween.OnUpdate = delegate(SourceTweenState t)
                {
                    if (goBack)
                    {
                        anchor = Vector2Interpolation.Lerp(end, start, t.Eased,
                            ((global::Celeste.Core.Mathematics.ISourceScalarEnvironment)engine).ScalarArithmetic);
                    }
                    else
                    {
                        anchor = Vector2Interpolation.Lerp(start, end, t.Eased,
                            ((global::Celeste.Core.Mathematics.ISourceScalarEnvironment)engine).ScalarArithmetic);
                    }
                };
                tween.OnComplete = delegate { goBack = !goBack; };
                OwnedComponents.Add(tween);
            }

            UpdatePosition();
            OwnedComponents.Add(hitWiggler =
                ((global::Celeste.Scene.Components.Oscillation.ISourceWigglerPoolScope)engine).WigglerPool.Create(1.2f,
                    2f,
                    delegate { spriteEvil.Position = hitDir * hitWiggler.Value * 8f; }));
            if (!NotCoreMode) OwnedComponents.Add(new CoreModeListenerState(OnChangeMode));
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            fireMode = !NotCoreMode && ((OriginalLevelSceneRoot)Scene).CoreMode == RoomCoreMode.Hot;
            spriteEvil.Visible = fireMode;
            sprite.Visible = !fireMode;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void OnChangeMode(RoomCoreMode coreMode)
        {
            fireMode = coreMode == RoomCoreMode.Hot;
            spriteEvil.Visible = fireMode;
            sprite.Visible = !fireMode;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void UpdatePosition()
        {
            Position = anchor;
            if (Wobbling) Position += new Vector2(sine.Value * 3f, sine.ValueOverTwo * 2f);
        }

        public override void Update()
        {
            base.Update();
            if (respawnTimer > 0f)
            {
                respawnTimer -= engine.DeltaTime;
                if (respawnTimer <= 0f)
                {
                    light.Visible = true;
                    bloom.Visible = true;
                    sprite.Play("on");
                    spriteEvil.Play("on");
                    if (!fireMode)
                        ((global::Celeste.Core.Audio.ISourceSpatialAudioEnvironment)engine).SpatialAudio.Play(
                            "event:/game/06_reflection/pinballbumper_reset", Position);
                }
            }
            else if (((Celeste.Scene.Tracking.ISourceScene)Scene).SourceScene.Clock.OnInterval(0.05f, engine))
            {
                float num = SourceRandomMath.NextAngle(
                    ((global::Celeste.Core.Randomness.ISourceRandomEnvironment)engine)
                    .Random);
                var type = (fireMode
                    ? ((global::Celeste.Game.Execution.Processes.IOriginalGameplayProcess)engine).ParticleTypes.Bumper
                    .P_FireAmbience
                    : ((global::Celeste.Game.Execution.Processes.IOriginalGameplayProcess)engine).ParticleTypes.Bumper
                    .P_Ambience);
                float direction = (fireMode ? (-(float)Math.PI / 2f) : num);
                float length = (fireMode ? 12 : 8);
                ((OriginalLevelSceneRoot)Scene).Particles.Emit(type, 1,
                    base.Center + Vector2Geometry.AngleToVector(num, length), Vector2.One * 2f, direction);
            }

            UpdatePosition();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void OnPlayer(RoomPlayerSession player)
        {
            if (fireMode && !((global::Celeste.Game.Execution.Processes.IOriginalGameplayProcess)engine).SaveContext
                    .CurrentSave.Assists.Invincible)
                HitHotPlayerCore(player);
            else LaunchPlayerCore(player);
        }

        private void HitHotPlayerCore(RoomPlayerSession player)
        {
            Vector2 vector =
                ((global::Celeste.Core.Mathematics.ISourceVectorEnvironment)engine).VectorArithmetic
                .SafeNormalize(
                    player.Center - base.Center);
            hitDir = -vector;
            hitWiggler.Start();
            ((global::Celeste.Core.Audio.ISourceSpatialAudioEnvironment)engine).SpatialAudio.Play(
                "event:/game/09_core/hotpinball_activate", Position);
            respawnTimer = respawnTime;
            player.Die(vector);
            ((OriginalLevelSceneRoot)Scene).Particles.Emit(
                ((global::Celeste.Game.Execution.Processes.IOriginalGameplayProcess)engine).ParticleTypes
                .Bumper.P_FireHit, 12, base.Center + vector * 12f, Vector2.One * 3f,
                Vector2Geometry.Angle(vector));
        }

        private void LaunchPlayerCore(RoomPlayerSession player)
        {
            if (!(respawnTimer <= 0f)) return;
            ((global::Celeste.Core.Audio.ISourceSpatialAudioEnvironment)engine).SpatialAudio.Play(
                "event:/game/06_reflection/pinballbumper_hit", Position);

            respawnTimer = respawnTime;
            Vector2 vector2 = player.ExplodeLaunch(Position, snapUp: false);
            sprite.Play("hit", restart: true);
            spriteEvil.Play("hit", restart: true);
            light.Visible = false;
            bloom.Visible = false;
            ((OriginalLevelSceneRoot)Scene).DirectionalShake(vector2, 0.15f);
            ((OriginalLevelSceneRoot)Scene).Displacement.AddBurst(base.Center, 0.3f, 8f, 32f, 0.8f);
            ((OriginalLevelSceneRoot)Scene).Particles.Emit(
                ((global::Celeste.Game.Execution.Processes.IOriginalGameplayProcess)engine).ParticleTypes.Bumper
                .P_Launch, 12, base.Center + vector2 * 12f, Vector2.One * 3f,
                Vector2Geometry.Angle(vector2));
        }

        public static SceneEntityState Load(OriginalLevelSceneRoot level,
            Celeste.Game.Content.Maps.Levels.OriginalLevelRoomData room, Vector2 offset,
            Celeste.Game.Content.Maps.Entities.OriginalLevelEntityData data)
            => new StaticBumper(level.EngineContext, data, offset);
    }
}
