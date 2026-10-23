using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Celeste.Mods.FrostHelper.Runtime.Expressions;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Conditions;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Commands;
using Session = Celeste.Simulation.Runtime.Session.ChapterCompletionSession;
using Entity = Celeste.Scene.Entities.SceneEntityState;

namespace Celeste.Mods.FrostHelper.Runtime.Expressions.Contexts
{
    internal static class EntityContext
    {
        public static ExpressionContext Create(FrostConditionRuntime runtime) => new ExpressionContext(runtime,
            new Dictionary<string, Condition>(), new Dictionary<string, FunctionCommand>());
    }
}
