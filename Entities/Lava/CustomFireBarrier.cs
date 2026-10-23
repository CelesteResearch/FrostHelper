// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Mathematics;
using Celeste.Core.Randomness;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Modules.Runtime;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Startup;
using Celeste.Game.Gameplay.Environment.Lava;
using Celeste.Mods.FrostHelper.Components.Flags;
using Celeste.Mods.FrostHelper.Components.Lava;
using Celeste.Mods.FrostHelper.Modules;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Entities;
using Celeste.Simulation.Audio.Sources;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Environment.Core;
using Celeste.Simulation.Gameplay.Platforms.Collision;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Player.Collision.Components;
using Celeste.Simulation.Gameplay.Player.Intro;

namespace Celeste.Mods.FrostHelper.Entities.Lava
{
    [CustomEntity("FrostHelper/CustomFireBarrier")]
    public class CustomFireBarrier : SceneEntityState, ISourceEngineContextEntity
    {
        private readonly IOriginalLavaProcess engine;
        public bool IsIce;
        public bool IgnoreCoreMode;
        private readonly bool canBeCollidable;
        private readonly bool hasSolid;
        internal readonly CustomLavaRect Lava;
        private SourceSolidEntityState solid;
        private SoundSourceComponentState idleSfx;
        public ISourceEngineContext EngineContext => engine;

        public static CustomFireBarrier Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) =>
            new(data, offset, (IOriginalLavaProcess)level.EngineContext);

        public CustomFireBarrier(EntityData data, Vector2 offset, IOriginalLavaProcess engine)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
            this.engine = engine;
            var module = ((ISourceEverestModulesEnvironment)engine).Modules.OfType<FrostHelperModule>().Single();
            float width = data.Width, height = data.Height;
            IsIce = data.Bool("isIce");
            canBeCollidable = data.Bool("canCollide", true);
            hasSolid = data.Bool("hasSolid", true);
            Tags = ((IOriginalGameTagsEnvironment)engine).Tags.TransitionUpdate.Value;
            IgnoreCoreMode = data.Bool("ignoreCoreMode");
            if (canBeCollidable)
            {
                Collider = new HitboxColliderGeometry(width, height);
                Add(new PlayerColliderComponentState(OnPlayer));
            }

            if (!IgnoreCoreMode) Add(new CoreModeListenerState(OnChangeMode));
            Add(Lava = new CustomLavaRect(width, height, IsIce ? 2 : 4,
                data.Attr("bubbleAmountMultiplier", "1"), engine, module));
            Lava.SurfaceColor = module.Colors.Read(data.Attr("surfaceColor", ""));
            Lava.EdgeColor = module.Colors.Read(data.Attr("edgeColor", ""));
            Lava.CenterColor = module.Colors.Read(data.Attr("centerColor", ""));
            string waves = data.Attr("waves", "default");
            if (waves == "default")
            {
                float small = data.Float("smallWaveAmplitude", 2f);
                if (IsIce) small /= 2f;
                Lava.Waves = new List<CustomLavaRect.WaveData>(2)
                {
                    new(small, .25f, 4f), new(data.Float("bigWaveAmplitude", 1f), .05f, .5f)
                };
            }
            else Lava.Waves = module.GetLavaWaves(waves);

            Lava.CurveAmplitude = data.Float("curveAmplitude", 1f);
            Lava.OnlyMode = data.Enum("surfaces", CustomLavaRect.OnlyModes.All);
            if (IsIce)
            {
                Lava.UpdateMultiplier = 0f;
                Lava.Spikey = 3f;
            }

            Lava.IsRainbow = (CustomLavaRect.RainbowModes)data.Int("rainbow");
            Lava.Fade = data.Float("fade", 16f);
            Depth = -8500;
            string flag = data.Attr("silentFlag", "");
            if (!string.IsNullOrWhiteSpace(flag))
            {
                idleSfx = engine.SoundSources.Create();
                Add(new FlagListener(engine, flag, (_, _, value) => OnSilentFlagChange(value), true, true));
            }
            else if (!IsIce && !data.Bool("silent")) SetupIdleSfx();
        }

        private void OnSilentFlagChange(bool value)
        {
            ((OriginalLevelSceneRoot)Scene).Scheduler.OnEndOfFrame += () =>
            {
                if (value) SetupIdleSfx();
                else Remove(idleSfx);
            };
        }

        private void SetupIdleSfx()
        {
            idleSfx ??= engine.SoundSources.Create();
            Add(idleSfx);
            idleSfx.Position = engine.VectorArithmetic.Divide(new Vector2(Width, Height), 2f);
            if (idleSfx.EventName != null) idleSfx.Play(idleSfx.EventName);
        }

        public void SetCollidable()
        {
            Collidable = IgnoreCoreMode || ((OriginalLevelSceneRoot)Scene).CoreMode ==
                (IsIce ? RoomCoreMode.Cold : RoomCoreMode.Hot);
            if (solid != null) solid.Collidable = Collidable;
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            if (canBeCollidable && hasSolid)
            {
                var position = Position + new Vector2(2, 3);
                var worlds = ((ISourcePlatformCollisionScene)scene).SolidWorldFactory;
                solid = new SourceSolidEntityState(position, Width - 4f, Height - 5f, false,
                    worlds.Create(position, false));
                ((OriginalLevelSceneRoot)scene).Scheduler.QueueSourceEntity(solid);
            }

            SetCollidable();
            if (Collidable) idleSfx?.Play("event:/env/local/09_core/lavagate_idle");
        }

        private void OnChangeMode(RoomCoreMode mode)
        {
            SetCollidable();
            if (!Collidable)
            {
                var level = (OriginalLevelSceneRoot)Scene;
                Vector2 center = Center;
                for (int x = 0; x < Width; x += 4)
                for (int y = 0; y < Height; y += 4)
                {
                    Vector2 at = Position + new Vector2(x + 2, y + 2) +
                                 SourceRandomMath.Range(engine.Random, -Vector2.One * 2f, Vector2.One * 2f);
                    level.Particles.Emit(engine.ParticleTypes.FireBarrier.P_Deactivate, at,
                        Vector2Geometry.Angle(at - center));
                }

                idleSfx?.Stop(true);
            }
            else idleSfx?.Play("event:/env/local/09_core/lavagate_idle");
        }

        private void OnPlayer(RoomPlayerSession player)
            => player.Die(engine.VectorArithmetic.SafeNormalize(player.Center - Center), false, true);

        public override void Update()
        {
            Visible = Collidable;
            if (((OriginalLevelSceneRoot)Scene).Transitioning) idleSfx?.UpdateSfxPosition();
            else base.Update();
        }
    }
}
