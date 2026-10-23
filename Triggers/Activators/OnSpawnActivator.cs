using System.Numerics;
using System.Linq;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.FrostHelper.Components.Lifecycle;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Intro;

namespace Celeste.Mods.FrostHelper.Triggers.Activators
{
    [CustomEntity("FrostHelper/OnSpawnActivator")]
    [RuntimeTypeAlias("FrostHelper.Triggers.Activator.OnSpawnActivator")]
    public sealed class OnSpawnActivator : BaseActivator
    {
        private readonly bool activateOnTransition;

        public OnSpawnActivator(ISourceEngineContext engine, EntityData data, Vector2 offset) : base(engine, data,
            offset)
        {
            Collidable = false;
            Add(new PostAwakeHook(Activate));
            activateOnTransition = data.Bool("activateOnTransition", true);
        }

        private void Activate()
        {
            var level = (OriginalLevelSceneRoot)Scene;
            if (activateOnTransition || level.LastIntroType != PlayerIntroType.Transition)
            {
                ActivateAll(level.SourceScene.Entities.GetEntitiesTrackIfNeeded<RoomPlayerSession>()
                    .Cast<RoomPlayerSession>().FirstOrDefault());
            }

            RemoveSelf();
        }
    }
}
