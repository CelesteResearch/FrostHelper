using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Modules.Runtime;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Platforms.Cassette;
using Celeste.Core.Reflection.Runtime;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Triggers.Foundation;

namespace Celeste.Mods.FrostHelper.Triggers.Cassette
{
    [CustomEntity("FrostHelper/CassetteTempoTrigger")]
    [RuntimeTypeAlias("FrostHelper.CassetteTempoTrigger")]
    public class CassetteTempoTrigger : SourceTriggerEntityState
    {
        private readonly float tempo;
        private readonly bool resetOnLeave;
        private float previousTempo;

        public CassetteTempoTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset) : base(
            ((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration,
            data.Position + offset, data.Width, data.Height,
            ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            tempo = data.Float("Tempo", 1f);
            resetOnLeave = data.Bool("ResetOnLeave", false);
        }

        public override void OnEnter(RoomPlayerSession player)
        {
            var level = (OriginalLevelSceneRoot)Scene;
            previousTempo = level.CassetteBlockTempo;
            level.CassetteBlockTempo = tempo;
            SetManagerTempo(tempo);
        }

        public override void OnLeave(RoomPlayerSession player)
        {
            if (resetOnLeave)
            {
                ((OriginalLevelSceneRoot)Scene).CassetteBlockTempo = previousTempo;
                SetManagerTempo(previousTempo);
            }
        }

        public void SetManagerTempo(float value)
        {
            var level = (OriginalLevelSceneRoot)Scene;
            var manager = level.Scheduler.Entities.GetTrackedEntity<OriginalCassetteBlockManagerState>();
            if (manager != null) manager.TempoMultiplier = value;
        }
    }
}
