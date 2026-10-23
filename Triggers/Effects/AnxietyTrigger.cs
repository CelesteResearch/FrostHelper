using System;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Randomness;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Scene.Components.Oscillation;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Triggers.Foundation;

namespace Celeste.Mods.FrostHelper.Triggers.Effects
{
    [CustomEntity("FrostHelper/AnxietyTrigger")]
    [RuntimeTypeAlias("FrostHelper.AnxietyTrigger")]
    public class AnxietyTrigger : SourceTriggerEntityState
    {
        private readonly ISourceEngineContext engine;
        private readonly SineWaveState anxietySine;
        private readonly float mult;
        private float anxietyJitter;

        public AnxietyTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration,
                data.Position + offset, data.Width, data.Height, ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            this.engine = engine;
            mult = data.Float("multiplyer", 1f);
            Add(anxietySine = new SineWaveState(engine, .3f, 0f));
        }

        public override void OnStay(RoomPlayerSession player)
        {
            base.OnStay(player);
            if (((OriginalLevelSceneRoot)Scene).OnInterval(.1f))
                anxietyJitter = SourceRandomMath.Range(((ISourceRandomEnvironment)engine).Random, -.1f, .1f);
            engine.Effects.Anxiety = Math.Max(.2f, anxietyJitter + anxietySine.Value * .6f) * mult;
        }
    }
}
