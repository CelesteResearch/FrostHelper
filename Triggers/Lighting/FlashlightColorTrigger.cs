using System.Collections;
using System.Linq;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Modules.Runtime;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Mods.FrostHelper.Runtime.Lighting;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Triggers.Foundation;

namespace Celeste.Mods.FrostHelper.Triggers.Lighting
{
    [CustomEntity("FrostHelper/FlashlightColorTrigger")]
    [RuntimeTypeAlias("FrostHelper.FlashlightColorTrigger")]
    public class FlashlightColorTrigger : SourceTriggerEntityState
    {
        private readonly ISourceEngineContext engine;
        private readonly FrostFlashlightColors runtime;
        private readonly PackedColor color;
        private readonly float timer;
        private readonly bool persistent;
        private PackedColor previousColor;

        public FlashlightColorTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : this(engine, data, offset, ((ISourceEverestModulesEnvironment)engine).Modules
                .OfType<IFrostFlashlightEnvironment>().Single().Flashlights)
        {
        }

        public FlashlightColorTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset,
            FrostFlashlightColors runtime)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration,
                data.Position + offset, data.Width, data.Height, ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            this.engine = engine;
            this.runtime = runtime;
            color = runtime.Colors.Read(data.Attr("color", "ffffff"));
            timer = data.Float("time", -1f);
            persistent = data.Bool("persistent", true);
        }

        public override void OnEnter(RoomPlayerSession player)
        {
            base.OnEnter(player);
            previousColor = player.Light.Color;
            if (persistent) runtime.SetSessionColor(color);
            player.Light.Color = color;
            if (timer != -1f) OwnedComponents.Add(new SimulationCoroutine(engine, DelayedResetFlashlightColor(player)));
        }

        public IEnumerator DelayedResetFlashlightColor(RoomPlayerSession player)
        {
            yield return timer;
            if (player.Light.Color == color)
            {
                if (persistent) runtime.SetSessionColor(previousColor);
                player.Light.Color = previousColor;
            }
        }
    }
}
