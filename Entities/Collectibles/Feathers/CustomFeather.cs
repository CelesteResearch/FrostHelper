using System.Collections;
using System.Linq;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Diagnostics.Logging;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest;
using Celeste.Everest.Modules.Runtime;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Gameplay.Effects.Slashes;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Graphics.Display;
using Celeste.Graphics.Rendering;
using Celeste.Input.Gameplay;
using Celeste.Input.Gameplay.Feedback;
using Celeste.Mods.FrostHelper.Collider.Spinners;
using Celeste.Mods.FrostHelper.Modules;
using Celeste.Mods.FrostHelper.Runtime.Feathers;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Components.Oscillation;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Collision.Components;
using Celeste.Simulation.Presentation.Lighting;
using Celeste.Simulation.Presentation.Particles.Definitions;
using Celeste.Simulation.Presentation.PostProcessing.Bloom;

namespace Celeste.Mods.FrostHelper.Entities.Collectibles.Feathers
{
    [CustomEntity("FrostHelper/CustomFeather = Load")]
    [RuntimeTypeAlias("FrostHelper.CustomFeather")]
    [SceneTracked(false)]
    public sealed class CustomFeather : SceneEntityState
    {
        internal enum Versions
        {
            Original,
            FixedHardcodedLowSpeed
        }

        internal enum AimInvertions
        {
            None,
            InvertY,
            InvertX,
            InvertBoth
        }

        public float MaxSpeed;
        public float LowSpeed;
        public float NeutralSpeed;
        public PackedColor FlyColor;
        public float FlyTime;
        public ParticleTypePresentation P_Collect;
        public ParticleTypePresentation P_Boost;
        public ParticleTypePresentation P_Flying;
        internal bool RefillStamina { get; }
        internal bool RefillDashes { get; }
        internal Versions Version { get; }
        internal AimInvertions InvertAim { get; }

        private readonly float RespawnTime;
        private readonly ISourceEngineContext engine;
        private readonly FrostFeatherRuntime runtime;
        private readonly Sprite sprite;
        private readonly SpriteImage outline;
        private readonly WigglerComponent wiggler;
        private readonly BloomPointState bloom;
        private readonly VertexLightState light;
        private readonly SineWaveState sine;
        private readonly bool shielded;
        private readonly bool singleUse;
        private readonly WigglerComponent shieldRadiusWiggle;
        private readonly WigglerComponent moveWiggle;
        private Vector2 moveWiggleDir;
        private OriginalLevelSceneRoot level;
        private float respawnTimer;
        private IOriginalGameplayProcess Process => (IOriginalGameplayProcess)engine;

        public static SceneEntityState Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) => new CustomFeather(data, offset, level.EngineContext);

        public CustomFeather(EntityData data, Vector2 offset, ISourceEngineContext engine)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
            this.engine = engine;
            runtime = FrostFeatherRuntime.For(engine);
            runtime.Load();
            Version = (Versions)data.Int("version", 0);
            shielded = data.Bool("shielded", false);
            singleUse = data.Bool("singleUse", false);
            RespawnTime = data.Float("respawnTime", 3f);
            var colors = ((ISourceEverestModulesEnvironment)engine).Modules.OfType<FrostHelperModule>().Single().Colors;
            FlyColor = colors.Read(data.Attr("flyColor", "ffd65c"));
            FlyTime = data.Float("flyTime", 2f);
            MaxSpeed = data.Float("maxSpeed", 190f);
            LowSpeed = data.Float("lowSpeed", 140f);
            NeutralSpeed = data.Float("neutralSpeed", 91f);
            Collider = SpinnerHitboxReader.Create(data.Attr("hitbox", ""), 1f,
                ((ISourceVectorEnvironment)engine).VectorArithmetic,
                message => ((IEverestLoggerProcessEnvironment)engine).Logger.Log(EverestLogLevel.Error,
                    "FrostHelper", message)) ?? new HitboxColliderGeometry(20f, 20f, -10f, -10f);
            Add(new PlayerColliderComponentState(OnPlayer));
            string path = data.Attr("spritePath", "objects/flyFeather/").Replace('\\', '/');
            if (!path.EndsWith('/')) path += "/";
            var atlas = ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas;
            sprite = new Sprite(engine, atlas, path) { Visible = true };
            sprite.CenterOrigin();
            sprite.Color = colors.Read(data.Attr("spriteColor", "White"));
            sprite.Justify = new Vector2(.5f, .5f);
            sprite.Add("loop", "idle", .06f, "flash");
            sprite.Add("flash", "flash", .06f, "loop");
            sprite.Play("loop");
            Add(sprite);
            var wigglers = ((ISourceWigglerPoolScope)engine).WigglerPool;
            Add(wiggler = wigglers.Create(1f, 4f, v => sprite.Scale = Vector2.One * (1f + v * .2f)));
            Add(bloom = new BloomPointState(.5f, 20f));
            Add(light = new VertexLightState(engine, PackedColor.White, 1f, 16, 48));
            Add(sine = new SineWaveState(engine, .6f, 0f).Randomize());
            Add(outline = new SpriteImage(atlas[path + "outline"]));
            outline.CenterOrigin();
            outline.Visible = false;
            Add(shieldRadiusWiggle = wigglers.Create(.5f, 4f));
            Add(moveWiggle = wigglers.Create(.8f, 2f));
            moveWiggle.StartZero = true;
            UpdateY();
            var particles = Process.ParticleTypes.FlyFeather;
            P_Collect = new ParticleTypePresentation(particles.P_Collect)
                { ColorMode = ParticleTypePresentation.ColorModes.Static, Color = FlyColor };
            P_Flying = new ParticleTypePresentation(particles.P_Flying)
                { ColorMode = ParticleTypePresentation.ColorModes.Static, Color = FlyColor };
            P_Boost = new ParticleTypePresentation(particles.P_Boost)
                { ColorMode = ParticleTypePresentation.ColorModes.Static, Color = FlyColor };
            RefillStamina = data.Bool("refillStamina", true);
            RefillDashes = data.Bool("refillDashes", true);
            InvertAim = data.Enum("invertAim", AimInvertions.None);
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            level = (OriginalLevelSceneRoot)Scene;
        }

        public override void Update()
        {
            base.Update();
            if (respawnTimer > 0f)
            {
                respawnTimer -= engine.DeltaTime;
                if (respawnTimer <= 0f) Respawn();
            }

            UpdateY();
            light.Alpha = ScalarMath.Approach(light.Alpha, sprite.Visible ? 1f : 0f, 4f * engine.DeltaTime,
                ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
            bloom.Alpha = light.Alpha * .8f;
        }

        public override void Render(ISourceRenderContext context)
        {
            base.Render(context);
            if (shielded && sprite.Visible)
                SourcePrimitiveDrawing.Circle(context, this, Position + sprite.Position,
                    10f - shieldRadiusWiggle.Value * 2f, PackedColor.White, 3,
                    ((ISourceVectorEnvironment)engine).VectorArithmetic);
        }

        private void Respawn()
        {
            if (!Collidable)
            {
                outline.Visible = false;
                Collidable = true;
                sprite.Visible = true;
                wiggler.Start();
                Play("event:/game/06_reflection/feather_reappear");
                level.ParticlesFG.Emit(Process.ParticleTypes.FlyFeather.P_Respawn, 16, Position,
                    Vector2.One * 2f, FlyColor);
            }
        }

        private void UpdateY()
        {
            sprite.Position.X = 0f;
            sprite.Position.Y = bloom.Y = sine.Value * 2f;
            sprite.Position += moveWiggleDir * moveWiggle.Value * -8f;
        }

        private void OnPlayer(RoomPlayerSession player)
        {
            Vector2 speed = player.Speed;
            if (shielded && !player.DashAttacking)
            {
                player.PointBounce(Center);
                moveWiggle.Start();
                shieldRadiusWiggle.Start();
                moveWiggleDir = ((ISourceVectorEnvironment)engine).VectorArithmetic.SafeNormalize(
                    Center - player.Center, Vector2.UnitY);
                Play("event:/game/06_reflection/feather_bubble_bounce");
                ((ISourceRumbleEnvironment)engine).RumbleSession.Rumble(1, 1);
            }
            else if (StartStarFly(player))
            {
                var state = runtime.ForPlayer(player);
                state.ApplyFlyColor(FlyColor);
                if (player.Machine.State != state.StateId && player.Machine.State != 19)
                    Play(shielded
                        ? "event:/game/06_reflection/feather_bubble_get"
                        : "event:/game/06_reflection/feather_get");
                else
                    Play(shielded
                        ? "event:/game/06_reflection/feather_bubble_renew"
                        : "event:/game/06_reflection/feather_renew");
                Collidable = false;
                Add(new SimulationCoroutine(engine, CollectRoutine(player, speed)));
                if (!singleUse)
                {
                    outline.Visible = true;
                    respawnTimer = RespawnTime;
                }
            }
        }

        public bool StartStarFly(RoomPlayerSession player) => runtime.ForPlayer(player).Start(this);

        internal Vector2 GetAim()
        {
            Vector2 value = ((ISourceGameInputEnvironment)engine).Input.Feather.Value;
            return InvertAim switch
            {
                AimInvertions.InvertBoth => -value,
                AimInvertions.InvertX => new Vector2(-value.X, value.Y),
                AimInvertions.InvertY => new Vector2(value.X, -value.Y),
                _ => value
            };
        }

        internal float GetLowSpeedHardcodedIfOld() => Version < Versions.FixedHardcodedLowSpeed ? 140f : LowSpeed;

        private IEnumerator CollectRoutine(RoomPlayerSession player, Vector2 playerSpeed)
        {
            level.Shake(.3f);
            sprite.Visible = false;
            yield return .05f;
            float direction = playerSpeed == Vector2.Zero
                ? Vector2Geometry.Angle(Position - player.Center)
                : Vector2Geometry.Angle(playerSpeed);
            level.ParticlesFG.Emit(P_Collect, 10, Position, Vector2.One * 6f, FlyColor);
            OriginalSlashFxState.Burst(engine, Position, direction);
        }

        private void Play(string path) => Process.AudioSession.Play(path, Position, Process.SaveContext);
    }
}
