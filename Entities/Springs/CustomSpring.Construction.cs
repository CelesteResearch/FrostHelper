using System;
using System.Globalization;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Content.Maps.Entities;
using System.Numerics;
using Celeste.Core.Randomness;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Game.Gameplay.Interaction.Springs;
using Celeste.Graphics.Animation;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Shapes;
using Celeste.Simulation.Gameplay.Platforms.StaticMovers;
using Celeste.Simulation.Gameplay.Player.Collision.Components;
using Celeste.Simulation.Gameplay.Holdables;
using Celeste.Simulation.Gameplay.Hazards.Puffer;
using Celeste.Simulation.Gameplay.Interaction.Springs;
using Celeste.Core.Scheduling;
using Celeste.Mods.FrostHelper.Components.Platforms;
using Celeste.Mods.FrostHelper.Runtime.Springs;

namespace Celeste.Mods.FrostHelper.Entities.Springs
{
    [RuntimeTypeAlias("FrostHelper.CustomSpring")]
    [CustomEntity("FrostHelper/SpringLeft = Load", "FrostHelper/SpringRight = Load", "FrostHelper/SpringFloor = Load",
        "FrostHelper/SpringCeiling = Load")]
    public partial class CustomSpring
    {
        public static CustomSpring Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data)
            => new(data, offset, (IOriginalSpringProcess)level.EngineContext,
                FrostSpringServices.For(level.EngineContext));

        private readonly FrostSpringServices services;
        internal FrostSpringServices Services => services;

        public CustomSpring(EntityData data, Vector2 offset, IOriginalSpringProcess process,
            FrostSpringServices services)
            : this(data, offset, EntityDataNameToOrientation[data.Name], process, services)
        {
        }

        public CustomSpring(EntityData data, Vector2 offset, CustomOrientations orientation,
            IOriginalSpringProcess process,
            FrostSpringServices services) : base(data.Position + offset, CustomToRegularOrientation[orientation],
            EntityAttributeReader.Bool(data.Values, "playerCanUse", true), process)
        {
            this.services = services;
            services.EnsureHooks();
            _version = EntityAttributeReader.Int(data.Values, "version", 0);
            var directory = EntityAttributeReader.Attr(data.Values, "directory", "objects/spring/");
            _renderOutline = EntityAttributeReader.Bool(data.Values, "renderOutline", true);
            _recovery = SpringRecovery.Read(data);
            var multiplier = EntityAttributeReader.Attr(data.Values, "speedMult", "1");
            var parts = multiplier.Split(',');
            if (!float.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out var x))
                SpeedMult = Vector2.One;
            else if (parts.Length == 1) SpeedMult = new Vector2(x);
            else if (float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var y))
                SpeedMult = new Vector2(x, y);
            else SpeedMult = Vector2.One;
            _multiplyPlayerY = orientation is not (CustomOrientations.WallLeft or CustomOrientations.WallRight) ||
                               multiplier.Contains(',');
            Orientation = orientation;
            OwnedComponents.Remove(OwnedComponents.Get<PlayerColliderComponentState>());
            OwnedComponents.Add(new PlayerColliderComponentState(NewOnCollide));
            OwnedComponents.Remove(OwnedComponents.Get<HoldableColliderState>());
            OwnedComponents.Add(new HoldableColliderState(NewOnHoldable));
            OwnedComponents.Remove(OwnedComponents.Get<PufferColliderState>());
            var pufferCollider = new PufferColliderState(NewOnPuffer);
            OwnedComponents.Add(pufferCollider);
            OwnedComponents.Remove(SpringSprite);
            SpringSprite = new Sprite(process, process.GameGraphics.GameAtlas, directory)
                { Color = services.Colors.Read(EntityAttributeReader.Attr(data.Values, "color", "White")) };
            OwnedComponents.Add(SpringSprite);
            Sprite.Add("idle", "", 0f, 0);
            Sprite.Add("bounce", "", .07f, new Chooser<string>("idle", 1f), 0, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 3, 4, 5);
            Sprite.Add("disabled", "white", .07f);
            Sprite.Play("idle");
            Sprite.Origin = new Vector2(Sprite.Width / 2f, Sprite.Height);
            OwnedComponents.Add(process.WigglerPool.Create(1f, 4f, v => Sprite.Scale.Y = 1f + v * .2f));
            var old = SpringStaticMover;
            var group = EntityAttributeReader.Int(data.Values, "attachGroup", -1);
            SourceStaticMoverState mover = group == -1
                ? new SourceStaticMoverState { OnAttach = old.OnAttach }
                : new GroupedStaticMover(group, true).SetOnAttach(old.OnAttach);
            mover.OnEnable = old.OnEnable;
            mover.OnDisable = old.OnDisable;
            mover.SolidChecker = old.SolidChecker;
            mover.JumpThruChecker = old.JumpThruChecker;
            switch (orientation)
            {
                case CustomOrientations.Floor:
                    Collider = new HitboxColliderGeometry(16f, 6f, -8f, -6f);
                    pufferCollider.Collider = new HitboxColliderGeometry(16f, 10f, -8f, -10f);
                    break;
                case CustomOrientations.WallLeft:
                    Collider = new HitboxColliderGeometry(6f, 16f, 0f, -8f);
                    pufferCollider.Collider = new HitboxColliderGeometry(12f, 16f, 0f, -8f);
                    Sprite.Rotation = (float)Math.PI / 2;
                    break;
                case CustomOrientations.WallRight:
                    Collider = new HitboxColliderGeometry(6f, 16f, -6f, -8f);
                    pufferCollider.Collider = new HitboxColliderGeometry(12f, 16f, -12f, -8f);
                    Sprite.Rotation = -(float)Math.PI / 2;
                    break;
                case CustomOrientations.Ceiling:
                    Collider = new HitboxColliderGeometry(16f, 6f, -8f, 0f);
                    pufferCollider.Collider = new HitboxColliderGeometry(16f, 10f, -8f, -4f);
                    Sprite.Rotation = (float)Math.PI;
                    mover.SolidChecker = other => SourceEntityCollision.CheckAt(this, other, Position - Vector2.UnitY);
                    mover.JumpThruChecker = other =>
                        SourceEntityCollision.CheckAt(this, other, Position - Vector2.UnitY);
                    break;
                default: throw new Exception("Orientation not supported!");
            }

            OwnedComponents.Remove(old);
            OwnedComponents.Add(mover);
            SpringStaticMover = mover;
            _oneUse = EntityAttributeReader.Bool(data.Values, "oneUse", false);
            if (_oneUse) OwnedComponents.Add(new SimulationCoroutine(process, OneUseParticleRoutine()));
            _alwaysActivate = EntityAttributeReader.Bool(data.Values, "alwaysActivate", false);
            BounceSfx = EntityAttributeReader.Attr(data.Values, "sfx", "event:/game/general/spring");
        }

        internal void BeforeBounce()
        {
            if (_version >= 5) OnSuccessfulHit();
        }
    }
}
