// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.FrostHelper.Runtime.Expressions;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Conditions;
using Celeste.Mods.FrostHelper.Runtime.Movement;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation.Gameplay.Triggers.Foundation;

namespace Celeste.Mods.FrostHelper.Triggers.Movement
{
    [CustomEntity("FrostHelper/NoMovementTrigger")]
    [RuntimeTypeAlias("FrostHelper.NoMovementTrigger")]
    [SceneTracked(false)]
    public sealed class NoMovementTrigger : SourceTriggerEntityState
    {
        private readonly bool mustBeInside;
        private readonly Condition condition;

        public NoMovementTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration,
                data.Position + offset, data.Width, data.Height, ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            NoMovementHooks.Load(engine);
            mustBeInside = data.Bool("mustBeInside", true);
            condition = FrostConditionRuntime.For(engine).GetCondition(data, "flag");
        }

        public static bool IsMovementDisabled(object scene)
        {
            if (!((ISourceScene)scene).SourceScene.Entities.TryGetTrackedEntities(typeof(NoMovementTrigger),
                    out List<object> triggers)) return false;
            foreach (NoMovementTrigger trigger in triggers)
                if ((!trigger.mustBeInside || trigger.Triggered) &&
                    (trigger.condition.Empty || trigger.condition.Check(((OriginalLevelSceneRoot)scene).Session)))
                    return true;
            return false;
        }
    }
}
