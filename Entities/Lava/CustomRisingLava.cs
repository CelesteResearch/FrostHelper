// SPDX-License-Identifier: MIT

using Celeste.Core.Scheduling;
using System;
using System.Linq;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Components.Animation;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Modules.Runtime;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Environment.Lava;
using Celeste.Graphics.Display.Colors;
using Celeste.Graphics.Effects.Liquids;
using Celeste.Mods.FrostHelper.Modules;
using Celeste.Mods.FrostHelper.Runtime.Lava;
using Celeste.Mods.FrostHelper.Triggers.Lava;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation;
using Celeste.Simulation.Audio.Sources;
using Celeste.Simulation.Gameplay.Environment.Core;
using Celeste.Simulation.Gameplay.Player.Collision.Components;

namespace Celeste.Mods.FrostHelper.Entities.Lava
{
    [CustomEntity("FrostHelper/CustomRisingLava")]
    [RuntimeTypeAlias("FrostHelper.CustomRisingLava")]
    [SceneTracked(false)]
    public sealed class CustomRisingLava : SceneEntityState
    {
        private readonly IOriginalLavaProcess engine;
        public bool ReverseCoreMode;
        public bool DoRubberbanding;
        private PackedColor[] Hot;
        private PackedColor[] Cold;
        private float Speed = -30f;
        private bool intro;
        private bool iceMode;
        public bool waiting;
        private float lerp;
        private LavaRectangleComponentState bottomRect;
        private float delay;
        private SoundSourceComponentState loopSfx;

        public CustomRisingLava(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : this((IOriginalLavaProcess)engine, data.Bool("intro"), data.Float("speed", -30f),
                GetColors(engine, data, true), GetColors(engine, data, false), data.Bool("reverseCoreMode"))
        {
            DoRubberbanding = data.Bool("doRubberbanding", true);
        }

        public CustomRisingLava(IOriginalLavaProcess engine, bool intro, float speed, PackedColor[] coldColors,
            PackedColor[] hotColors, bool reverseCoreMode)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration)
        {
            this.engine = engine;
            RisingLavaBlockerHooks.For(engine);
            delay = 0f;
            this.intro = intro;
            Speed = speed;
            Depth = -1000000;
            Collider = new HitboxColliderGeometry(340f, 120f);
            Visible = false;
            Add(new PlayerColliderComponentState(OnPlayer));
            Add(new CoreModeListenerState(OnChangeMode));
            Add(loopSfx = engine.SoundSources.Create());
            Add(bottomRect = new LavaRectangleComponentState(400f, 200f, 4, engine,
                "danger/lava/bubble_a", "particles/bubble"));
            bottomRect.Position = new Vector2(-40f, 0f);
            bottomRect.OnlyMode = LavaRectangleComponentState.OnlyModes.OnlyTop;
            bottomRect.SmallWaveAmplitude = 2f;
            Cold = coldColors;
            Hot = hotColors;
            ReverseCoreMode = reverseCoreMode;
        }

        public static PackedColor[] GetColors(ISourceEngineContext engine, EntityData data, bool cold)
        {
            var module = ((ISourceEverestModulesEnvironment)engine).Modules.OfType<FrostHelperModule>().Single();
            var colors = new PackedColor[3];
            for (int i = 0; i < colors.Length; i++)
                colors[i] = module.Colors.Read(data.Attr((cold ? "cold" : "hot") + "Color" + (i + 1), ""));
            return colors;
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            X = ((OriginalLevelSceneRoot)Scene).Bounds.Left - 10;
            Y = ((OriginalLevelSceneRoot)Scene).Bounds.Bottom + 16;
            iceMode = ((OriginalLevelSceneRoot)Scene).Session.CoreMode == RoomCoreMode.Cold;
            if (ReverseCoreMode) iceMode = !iceMode;
            loopSfx.Play("event:/game/09_core/rising_threat", "room_state", iceMode ? 1f : 0f);
            loopSfx.Position = new Vector2(Width / 2f, 0f);
            lerp = iceMode ? 1f : 0f;
            UpdateColors();
        }

        private RoomPlayerSession FindPlayer() =>
            ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntity<RoomPlayerSession>();

        public override void Awake(object scene)
        {
            base.Awake(scene);
            var player = FindPlayer();
            // Preserve the original pre-null-check collision query.
            foreach (CustomRisingLavaStartHeightTrigger trigger in ((ISourceScene)player.Scene).SourceScene.Entities
                     .GetTrackedEntities(typeof(CustomRisingLavaStartHeightTrigger)))
                if (SourceEntityCollision.Check(player, trigger))
                {
                    Y = trigger.Node.Y;
                    break;
                }

            if (intro)
            {
                waiting = true;
                Visible = true;
                UpdateColors();
            }
            else if (player != null && player.JustRespawned) waiting = true;
        }

        private void OnChangeMode(RoomCoreMode mode)
        {
            iceMode = mode == RoomCoreMode.Cold;
            if (ReverseCoreMode) iceMode = !iceMode;
            loopSfx.Param("room_state", iceMode ? 1f : 0f);
        }

        private void OnPlayer(RoomPlayerSession player)
        {
            if (engine.SaveContext.CurrentSave.Assists.Invincible)
            {
                if (delay <= 0f)
                {
                    float from = Y;
                    float to = Y + 48f;
                    player.Data.Motion.Normal.Speed.Y = -200f;
                    player.RefillDash();
                    SourceTweenState.Set(engine.TweenPool, OwnedComponents, SourceTweenState.TweenMode.Oneshot,
                        .4f, engine.EasingArithmetic.CubeOut,
                        tween => Y = ScalarMath.Lerp(from, to, tween.Eased, engine.ScalarArithmetic));
                    delay = .5f;
                    loopSfx.Param("rising", 0f);
                    engine.SpatialAudio.Play("event:/game/general/assist_screenbottom", player.Position);
                }
            }
            else player.Die(-Vector2.UnitY, false, true);
        }

        public override void Update()
        {
            delay -= engine.DeltaTime;
            X = ((OriginalLevelSceneRoot)Scene).Camera.X;
            var player = FindPlayer();
            base.Update();
            Visible = true;
            if (waiting)
            {
                loopSfx.Param("rising", 0f);
                if (!intro && player != null && player.JustRespawned)
                    Y = ScalarMath.Approach(Y, player.Y + 32f, 32f * engine.DeltaTime, engine.ExtremaArithmetic);
                if ((!iceMode || !intro) && (player == null || !player.JustRespawned)) waiting = false;
            }
            else
            {
                float multiplier = 1f;
                if (DoRubberbanding)
                {
                    float cameraBottom = ((OriginalLevelSceneRoot)Scene).Camera.Bottom - 12f;
                    if (Top > cameraBottom + 96f) Top = cameraBottom + 96f;
                    multiplier = !(Top > cameraBottom)
                        ? ScalarMath.ClampedMap(cameraBottom - Top, 0f, 32f, 1f, .5f)
                        : ScalarMath.ClampedMap(Top - cameraBottom, 0f, 96f, 1f, 2f);
                }

                if (delay <= 0f)
                {
                    loopSfx.Param("rising", 1f);
                    Y = Y + Speed * multiplier * engine.DeltaTime;
                }
            }

            UpdateColors();
        }

        private void UpdateColors()
        {
            lerp = ScalarMath.Approach(lerp, iceMode ? 1f : 0f, engine.DeltaTime * 4f, engine.ExtremaArithmetic);
            bottomRect.SurfaceColor = SourceColorInterpolation.Lerp(Hot[0], Cold[0], lerp, engine.ScalarArithmetic);
            bottomRect.EdgeColor = SourceColorInterpolation.Lerp(Hot[1], Cold[1], lerp, engine.ScalarArithmetic);
            bottomRect.CenterColor = SourceColorInterpolation.Lerp(Hot[2], Cold[2], lerp, engine.ScalarArithmetic);
            bottomRect.Spikey = lerp * 5f;
            bottomRect.UpdateMultiplier = (1f - lerp) * 2f;
            bottomRect.Fade = iceMode ? 128f : 32f;
        }
    }
}
