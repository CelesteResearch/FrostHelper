using System.Linq;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Geometry;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Scene.Entities;
using Celeste.Scene.Scheduling;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;

namespace Celeste.Mods.FrostHelper.Triggers.Visibility
{
    [SceneTracked(false)]
    [CustomEntity("FrostHelper/FlagIfVisibleTrigger")]
    [RuntimeTypeAlias("FrostHelper.FlagIfVisibleTrigger")]
    public class FlagIfVisibleTrigger : SceneEntityState
    {
        public string Flag;
        public int W;
        public int H;
        public FlagIfVisibleTrigger[] ChildTriggers;
        private OriginalLevelSceneRoot level;

        public FlagIfVisibleTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
            Flag = data.Attr("flag", "");
            Collidable = false;
            Visible = false;
            W = data.Width;
            H = data.Height;
        }

        public bool InView()
        {
            var camera = level.Camera.Position;
            return new IntRect((int)X, (int)Y, W, H).Intersects(
                new IntRect((int)camera.X, (int)camera.Y, 320, 180));
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            level = Scene as OriginalLevelSceneRoot;
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            if (Active)
            {
                ChildTriggers = ((ISourceScene)scene).SourceScene.Entities
                    .GetEntitiesTrackIfNeeded<FlagIfVisibleTrigger>().Cast<FlagIfVisibleTrigger>()
                    .Where(trigger => trigger != this && trigger.Active && trigger.Flag == Flag).ToArray();
                foreach (var trigger in ChildTriggers) trigger.Active = false;
            }
        }

        public override void Update()
        {
            bool inView = InView();
            if (!inView)
                foreach (var trigger in ChildTriggers)
                    if (inView = trigger.InView())
                        break;
            level.Session.SetFlag(Flag, inView);
        }
    }
}
