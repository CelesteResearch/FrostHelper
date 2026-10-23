// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Mods.FrostHelper.Runtime.Movement;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation.Gameplay.Triggers.Foundation;

namespace Celeste.Mods.FrostHelper.Triggers.Movement
{
    [CustomEntity("FrostHelper/ForcedFastfall")]
    [RuntimeTypeAlias("FrostHelper.ForcedFastfallTrigger")]
    [SceneTracked(false)]
    public class ForcedFastfallTrigger : SourceTriggerEntityState
    {
        public ForcedFastfallTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration,
                data.Position + offset, data.Width, data.Height, ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            ForcedFastfallHooks.Load(engine);
        }

        public static bool IsForcedFastfall(object scene)
        {
            if (!((ISourceScene)scene).SourceScene.Entities.TryGetTrackedEntities(typeof(ForcedFastfallTrigger),
                    out List<object> triggers)) return false;
            foreach (ForcedFastfallTrigger trigger in triggers)
                if (trigger.Triggered)
                    return true;
            return false;
        }
    }
}
