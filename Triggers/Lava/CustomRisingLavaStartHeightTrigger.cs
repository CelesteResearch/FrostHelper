// SPDX-License-Identifier: MIT

using Celeste.Core.Scheduling;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Triggers.Foundation;

namespace Celeste.Mods.FrostHelper.Triggers.Lava
{
    [CustomEntity("FrostHelper/CustomRisingLavaStartHeightTrigger")]
    [RuntimeTypeAlias("FrostHelper.CustomRisingLavaStartHeightTrigger")]
    [SceneTracked(false)]
    public class CustomRisingLavaStartHeightTrigger : SourceTriggerEntityState
    {
        public Vector2 Node;

        public CustomRisingLavaStartHeightTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration,
                data.Position + offset, data.Width, data.Height, ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            Node = data.NodesOffset(offset)[0];
        }
    }
}
