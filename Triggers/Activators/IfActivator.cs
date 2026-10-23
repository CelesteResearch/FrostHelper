using System.Numerics;
using Celeste.Core.Scheduling;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Mods.FrostHelper.Runtime.Expressions;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Conditions;
using Celeste.Simulation;

namespace Celeste.Mods.FrostHelper.Triggers.Activators
{
    [CustomEntity("FrostHelper/IfActivator")]
    [RuntimeTypeAlias("FrostHelper.Triggers.Activator.IfActivator")]
    internal sealed class IfActivator : BaseActivator, IIfActivator
    {
        private readonly Condition condition;
        public bool IsElse { get; }

        public IfActivator(ISourceEngineContext engine, EntityData data, Vector2 offset) : base(engine, data, offset)
        {
            condition = FrostConditionRuntime.For(engine).GetCondition(data, "condition");
            IsElse = data.Bool("isElse", false);
            Collidable = false;
        }

        public override void OnEnter(RoomPlayerSession player)
        {
            base.OnEnter(player);
            if (condition.Check()) ActivateAll(player);
            else ActiveElseBlocks(player);
        }
    }
}
