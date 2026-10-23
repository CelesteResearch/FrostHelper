using System;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Triggers.Foundation;

namespace Celeste.Mods.FrostHelper.Triggers.Movement
{
    [CustomEntity("FrostHelper/CapDashOnGroundTrigger")]
    [RuntimeTypeAlias("FrostHelper.Triggers.CapDashOnGroundTrigger")]
    public class CapDashOnGroundTrigger : SourceTriggerEntityState
    {
        public CapDashOnGroundTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration,
                data.Position + offset, data.Width, data.Height, ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
        }

        public override void OnStay(RoomPlayerSession player)
        {
            base.OnStay(player);
            if (player.OnGround(1))
                player.Data.Dashes = Math.Min(player.Data.Dashes, player.Motor.BeforeState.MaxDashes);
        }
    }
}
