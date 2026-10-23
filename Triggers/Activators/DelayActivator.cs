using System.Numerics;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Simulation;

namespace Celeste.Mods.FrostHelper.Triggers.Activators
{
    [CustomEntity("FrostHelper/DelayActivator")]
    [RuntimeTypeAlias("FrostHelper.Triggers.Activator.DelayActivator")]
    public sealed class DelayActivator : BaseActivator
    {
        public DelayActivator(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(engine, data, offset)
        {
            Collidable = false;
        }

        public override void OnEnter(RoomPlayerSession player)
        {
            base.OnEnter(player);
            ActivateAll(player);
        }
    }
}
