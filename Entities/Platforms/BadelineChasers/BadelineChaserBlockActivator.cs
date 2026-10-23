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
    [CustomEntity("FrostHelper/BadelineChaserBlockActivator")]
    [SceneTracked(false)]
    [RuntimeTypeAlias("FrostHelper.BadelineChaserBlockActivator")]
    public class BadelineChaserBlockActivator : SourceSolidEntityState
    {
        private readonly BadelineChaserBlockResources resources;
        public static string ActivateSfx = "event:/radleymctuneston_badelineblock_activate";
        public static string DeactivateSfx = "event:/radleymctuneston_badelineblock_deactivate";
        public static string BuzzSfx = "event:/radleymctuneston_badelineblock_buzz";
        public bool Solid;
        public Sprite Emblem;
        public bool DoneCollisionChecks;

        public static BadelineChaserBlockActivator Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) => new(data, offset, level.SolidWorldFactory);

        public BadelineChaserBlockActivator(EntityData data, Vector2 offset, ISourceSolidWorldFactory worlds)
            : base(data.Position + offset, data.Width, data.Height, false, worlds.Create(data.Position + offset, false))
        {
            var engine = SourceWorld.EngineContext;
            resources = BadelineChaserBlockResources.For(engine);
            Solid = data.Bool("solid", true);
            Collider = new HitboxColliderGeometry(data.Width, data.Height);
            Collidable = Solid;
            Emblem = new Sprite(engine, ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas,
                "objects/FrostHelper/badelineChaserBlock/emblem");
            Emblem.Add("loop", "field");
            Emblem.CenterOrigin();
            Emblem.Play("loop");
            Depth = 8990;
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            BadelineChaserBlockManager.Ensure(SourceWorld.EngineContext, (ISourceScene)scene, resources);
        }

        public bool HasBaddyRider()
        {
            var entities = ((ISourceScene)Scene).SourceScene.Entities;
            foreach (BadelineOldsiteState chaser in entities.GetEntitiesTrackIfNeeded<BadelineOldsiteState>())
                if (SourceEntityCollision.CheckAt(chaser, this, chaser.Position + Vector2.UnitY * 2f))
                    return true;
            return false;
        }

        public override void Render(ISourceRenderContext context)
        {
            base.Render(context);
            SwapBlockDrawing.DrawBlockStyle(context, this, Position, Width, Height,
                Solid ? resources.ActivatorSolid : resources.ActivatorField, Emblem, PackedColor.White);
        }
    }
}
