using System;
using System.Collections;
using System.Linq;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest;
using Celeste.Everest.Modules.Runtime;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Collectibles.Refills;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Display;
using Celeste.Mods.FrostHelper.Collider.Spinners;
using Celeste.Mods.FrostHelper.Modules;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Components.Oscillation;
using Celeste.Everest.Diagnostics.Logging;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Collision.Components;
using Celeste.Simulation.Presentation.Effects.Mirrors;
using Celeste.Simulation.Presentation.Lighting;
using Celeste.Simulation.Presentation.PostProcessing.Bloom;

namespace Celeste.Mods.FrostHelper.Entities.Refills
{
    [CustomEntity("FrostHelper/PlusOneRefill = Load")]
    [RuntimeTypeAlias("FrostHelper.PlusOneRefill")]
    internal sealed partial class PlusOneRefill : SceneEntityState
    {
        private readonly IOriginalRefillProcess engine;
        private readonly ProcessRefillGlowParticleSource glowSource;
        private readonly int dashCount;
        private readonly float respawnTime;
        private readonly PackedColor particleColor;
        private readonly bool recoverStamina;
        private readonly Sprite sprite;
        private readonly Sprite flash;
        private readonly SpriteImage outline;
        private readonly WigglerComponent wiggler;
        private readonly BloomPointState bloom;
        private readonly VertexLightState light;
        private readonly SineWaveState sine;
        private readonly bool oneUse;
        private readonly Action respawnAction;
        private IOriginalRefillScene level;
        private float respawnTimer;

        public static SceneEntityState Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) => new PlusOneRefill(level.EngineContext, data, offset);

        public PlusOneRefill(ISourceEngineContext context, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)context).EntitySourceDataRegistration, data.Position + offset)
        {
            engine = (IOriginalRefillProcess)context;
            glowSource = new ProcessRefillGlowParticleSource(engine);
            respawnAction = Respawn;
            var module = ((ISourceEverestModulesEnvironment)context).Modules.OfType<FrostHelperModule>().Single();
            oneUse = data.Bool("oneUse");
            dashCount = data.Int("dashCount", 1);
            respawnTime = data.Float("respawnTime", 2.5f);
            Collider = SpinnerHitboxReader.Create(data.Attr("hitbox", ""), 1f,
                           ((ISourceVectorEnvironment)context).VectorArithmetic,
                           message => ((IEverestLoggerProcessEnvironment)context).Logger.Log(EverestLogLevel.Error,
                               "FrostHelper", message))
                       ?? new HitboxColliderGeometry(16f, 16f, -8f, -8f);
            particleColor = module.Colors.Read(data.Attr("particleColor", "ffffff"));
            recoverStamina = data.Bool("recoverStamina");
            string directory = data.Attr("directory", "objects/FrostHelper/plusOneRefill");
            var atlas = engine.GameGraphics.GameAtlas;
            OwnedComponents.Add(new PlayerColliderComponentState(OnPlayer));
            OwnedComponents.Add(outline = new SpriteImage(atlas[directory + "/outline"]));
            outline.CenterOrigin();
            outline.Visible = false;
            OwnedComponents.Add(sprite = new Sprite(engine, atlas, directory + "/idle"));
            sprite.AddLoop("idle", "", .1f);
            sprite.Play("idle");
            sprite.CenterOrigin();
            OwnedComponents.Add(flash = new Sprite(engine, atlas, directory + "/flash"));
            flash.Add("flash", "", .05f);
            flash.OnFinish = delegate { flash.Visible = false; };
            flash.CenterOrigin();
            OwnedComponents.Add(wiggler = ((ISourceWigglerPoolScope)engine).WigglerPool.Create(1f, 4f,
                value => sprite.Scale = flash.Scale = Vector2.One * (1f + value * .2f)));
            OwnedComponents.Add(new MirrorReflectionState());
            OwnedComponents.Add(bloom = RefillLightingParameters.Bloom(data.Attr("bloom", "")));
            OwnedComponents.Add(light = RefillLightingParameters.Light(context, data.Attr("light", ""), module.Colors));
            OwnedComponents.Add(sine = new SineWaveState(engine, .6f));
            sine.Randomize();
            RefillPresentation.UpdateY(sprite, flash, bloom, sine);
            presentation = RefillPresentation.BindPresentation(this.engine, new PresentationPhases(this));
            Depth = -11001;
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            level = Scene as IOriginalRefillScene;
        }

        public override void Update()
        {
            base.Update();
            presentation.Update();
        }

        private void Respawn() => RefillPresentation.Respawn(this, engine, level, sprite, outline, wiggler,
            engine.RefillParticles.P_Regen, "event:/game/general/diamond_return", -11001, particleColor);

        public override void Render(Celeste.Core.Rendering.ISourceRenderContext context)
        {
            if (sprite != null) RefillPresentation.RenderOutline(context, sprite);
            base.Render(context);
        }

        private void OnPlayer(RoomPlayerSession player)
        {
            if (player.Data.Dashes < player.Motor.BeforeState.MaxDashes ||
                (recoverStamina && player.Data.Stamina < 20f))
            {
                player.Data.Dashes = Math.Min(player.Data.Dashes + dashCount, player.Motor.BeforeState.MaxDashes);
                if (recoverStamina) player.RefillStamina();
                engine.SpatialAudio.Play("event:/game/general/diamond_touch", Position);
                engine.RumbleSession.Rumble(1, 1);
                Collidable = false;
                OwnedComponents.Add(new SimulationCoroutine(engine, RefillRoutine(player)));
                respawnTimer = respawnTime;
            }
        }

        private IEnumerator RefillRoutine(RoomPlayerSession player)
        {
            RefillPresentation.BeginRoutine(engine);
            yield return null;
            level.Shake();
            RefillPresentation.HideForRoutine(this, sprite, flash, outline, oneUse);
            yield return .05f;
            RefillPresentation.FinishRoutine(this, engine, level, engine.RefillParticles.P_Shatter,
                Vector2Geometry.Angle(player.Speed), particleColor);
            if (oneUse) RemoveSelf();
        }
    }
}
