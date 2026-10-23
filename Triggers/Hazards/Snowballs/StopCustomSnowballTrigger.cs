using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Mods.FrostHelper.Entities.Hazards.Snowballs;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Triggers.Foundation;

namespace Celeste.Mods.FrostHelper.Triggers.Hazards.Snowballs
{
    [CustomEntity("FrostHelper/StopCustomSnowballTrigger")]
    [RuntimeTypeAlias("FrostHelper.StopCustomSnowballTrigger")]
    public sealed class StopCustomSnowballTrigger : SourceTriggerEntityState
    {
        private readonly bool once;

        public StopCustomSnowballTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration,
                data.Position + offset, data.Width, data.Height, ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
            => once = data.Bool("once", false);

        public override void OnEnter(RoomPlayerSession player)
        {
            base.OnEnter(player);
            bool found = false;
            if (((ISourceScene)Scene).SourceScene.Entities.TryGetTrackedEntities(typeof(CustomSnowball),
                    out var entities))
                foreach (CustomSnowball snowball in entities)
                {
                    snowball.StartLeaving();
                    found = true;
                }

            if (found && once) RemoveSelf();
        }
    }
}
