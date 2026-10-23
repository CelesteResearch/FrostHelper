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
    public interface IExpressionContext
    {
        FrostConditionRuntime Runtime { get; }
        bool TryGetSimpleCommand(string name, [NotNullWhen(true)] out Condition? command);
        bool TryGetFunctionCommand(string name, [NotNullWhen(true)] out FunctionCommand? factory);
        void RegisterSimpleCommand(string name, Condition command);
        void RegisterFunctionCommand(string name, FunctionCommand factory);
    }
}
