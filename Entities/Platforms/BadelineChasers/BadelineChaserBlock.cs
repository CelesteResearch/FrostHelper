using Celeste.Mods.FrostHelper.Runtime.BadelineChasers;
using Celeste.Mods.FrostHelper.Entities.Controllers.BadelineChasers;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using System;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Geometry;
using Celeste.Core.Rendering;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Atlas;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Platforms.Collision;
using Celeste.Simulation.Gameplay.Hazards.Spikes;
using Celeste.Game.Gameplay.Interaction.Springs;
using Celeste.Game.Gameplay.Enemies.Badeline.Chasers;
using Celeste.Game.Entities.Platforms.SwapBlocks.Rendering;

namespace Celeste.Mods.FrostHelper.Entities.Platforms.BadelineChasers
{
    [CustomEntity("FrostHelper/BadelineChaserBlock")]
    [SceneTracked(false)]
    [RuntimeTypeAlias("FrostHelper.BadelineChaserBlock")]
    public class BadelineChaserBlock : SourceSolidEntityState
    {
        private readonly BadelineChaserBlockResources resources;
        private bool pressed;
        public bool Reversed;
        public Sprite Emblem;
        private readonly bool giveLiftBoost;
        private bool justChangedState;

        public static BadelineChaserBlock Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) => new(data, offset, level.SolidWorldFactory);

        public BadelineChaserBlock(EntityData data, Vector2 offset, ISourceSolidWorldFactory worlds)
            : base(data.Position + offset, data.Width, data.Height, false, worlds.Create(data.Position + offset, false))
        {
            var engine = SourceWorld.EngineContext;
            resources = BadelineChaserBlockResources.For(engine);
            Reversed = data.Bool("reversed", false);
            Collider = new HitboxColliderGeometry(data.Width, data.Height);
            Emblem = new Sprite(engine, ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas,
                "objects/FrostHelper/badelineChaserBlock/emblem");
            Emblem.Add("solid", !Reversed ? "solidreverse" : "solid");
            Emblem.Add("pressed", !Reversed ? "pressedreverse" : "pressed");
            Emblem.CenterOrigin();
            Emblem.Play(Reversed ? "pressed" : "solid");
            AllowStaticMovers = true;
            Depth = -9000;
            giveLiftBoost = data.Bool("giveLiftBoost", true);
        }

        private void ShiftSize(int amount)
        {
            if (giveLiftBoost) MoveV(amount);
            else MoveV(amount, 0f);
        }

        public void SetState(bool state)
        {
            if (pressed != state)
            {
                pressed = state;
                ShiftSize(-1);
                justChangedState = true;
            }
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            var disabledColor = new PackedColor(0x72, 0x4f, 0x86);
            foreach (var mover in staticMovers)
            {
                if (mover.Entity is SpikesState spikes)
                {
                    spikes.EnabledColor = PackedColor.White;
                    spikes.DisabledColor = disabledColor;
                    spikes.VisibleWhenDisabled = true;
                }
                else if (mover.Entity is OriginalSpringState spring)
                {
                    spring.DisabledColor = disabledColor;
                    spring.VisibleWhenDisabled = true;
                }
            }
        }

        public override void Update()
        {
            if (justChangedState)
            {
                ShiftSize(1);
                justChangedState = false;
            }

            var player = ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntity<RoomPlayerSession>();
            bool collidable = Collidable;
            Collidable = true;
            if (player == null || !SourceEntityCollision.Check(player, this))
            {
                Collidable = pressed == Reversed;
                if (pressed ^ Reversed) DisableStaticMovers();
                else EnableStaticMovers();
            }
            else Collidable = collidable;

            if (!Collidable) Depth = 8990;
            else if (player != null && player.Top >= Bottom - 1f) Depth = 10;
            else Depth = -10;
            foreach (var mover in staticMovers)
                ((Celeste.Scene.Entities.SceneEntityState)mover.Entity).Depth = Depth + 1;
            base.Update();
        }

        public override void Render(ISourceRenderContext context)
        {
            base.Render(context);
            Emblem.Play(Collidable ? "solid" : "pressed");
            SwapBlockDrawing.DrawBlockStyle(context, this, Position, Width, Height,
                Collidable ? resources.Solid : resources.Pressed, Emblem, PackedColor.White);
        }
    }
}
