// SPDX-License-Identifier: MIT

using System;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Geometry;
using Celeste.Core.Mathematics;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest;
using Celeste.Everest.Diagnostics.Logging;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Presentation.Doors;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Mods.FrostHelper.Collider.Spinners;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Collision.Blockers;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Player.Collision.Components;
using Celeste.Simulation.Presentation.Lighting;

namespace Celeste.Mods.FrostHelper.Entities.Doors
{
    [CustomEntity("FrostHelper/StaticDoor")]
    [SceneTracked]
    [RuntimeTypeAlias("FrostHelper.StaticDoor")]
    public class StaticDoor : SceneEntityState, IDoorPresentationSource
    {
        private readonly ISourceEngineContext engine;
        public Sprite Sprite;
        Sprite IDoorPresentationSource.Sprite => Sprite;
        public string OpenSfx;
        public string CloseSfx;
        public LightOccludeState Occlude;
        public bool Disabled;
        public bool SolidIfDisabled;

        public StaticDoor(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
            this.engine = engine;
            Depth = 8998;
            string type = data.Attr("type", "wood");
            if (type == "wood")
            {
                Add(Sprite = ((ISourceGameGraphicsEnvironment)engine).GameGraphics.SpriteFactory.Create("door"));
                OpenSfx = "event:/game/03_resort/door_wood_open";
                CloseSfx = "event:/game/03_resort/door_wood_close";
            }
            else
            {
                Add(Sprite = ((ISourceGameGraphicsEnvironment)engine).GameGraphics.SpriteFactory.Create(type + "door"));
                OpenSfx = "event:/game/03_resort/door_metal_open";
                CloseSfx = "event:/game/03_resort/door_metal_close";
            }

            string openSound = data.Attr("openSfx", null);
            if (!string.IsNullOrEmpty(openSound)) OpenSfx = openSound;
            string closeSound = data.Attr("closeSfx", null);
            if (!string.IsNullOrEmpty(closeSound)) CloseSfx = closeSound;
            SolidIfDisabled = data.Bool("solidIfDisabled", false);
            Sprite.Play("idle");
            Collider = SpinnerHitboxReader.Create(data.Attr("hitbox", ""), 1f,
                           ((ISourceVectorEnvironment)engine).VectorArithmetic,
                           warning => ((IEverestLoggerProcessEnvironment)engine).Logger.Log(EverestLogLevel.Warn,
                               "FrostHelper", warning))
                       ?? new HitboxColliderGeometry(12f, 22f, -6f, -23f);
            Add(Occlude = new LightOccludeState(new IntRect(-1, -24, 2, 24), data.Float("lightOccludeAlpha", 1f)));
            Add(new PlayerColliderComponentState(HitPlayer));
        }

        private void HitPlayer(RoomPlayerSession player)
        {
            if (!Disabled) Open(player.X);
        }

        public void Disable()
        {
            if (Disabled) return;
            Disabled = true;
            if (!SolidIfDisabled) return;
            var level = (OriginalLevelSceneRoot)Scene;
            var position = Position - new Vector2(1f, 24f);
            var solid = new SourceSolidEntityState(position, 3f, 24f, false,
                level.SolidWorldFactory.Create(position, false));
            solid.Add(new ClimbBlockerState(true));
            ((ISourceScene)Scene).SourceScene.QueueSourceEntity(solid);
        }

        public void Open(float x) => DoorPresentation.Open((IOriginalGameplayProcess)engine, this, x, OpenSfx);

        public override void Update()
        {
            string previousAnimation = Sprite.CurrentAnimationID;
            base.Update();
            Occlude.Visible = Sprite.CurrentAnimationID == "idle";
            DoorPresentation.PlayCloseIfCompleted((IOriginalGameplayProcess)engine, this,
                previousAnimation, CloseSfx);
        }

        public static SceneEntityState Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data) => new StaticDoor(level.EngineContext, data, offset);
    }
}
