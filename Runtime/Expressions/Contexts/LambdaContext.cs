using System;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Commands;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Diagnostics.CodeAnalysis;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Operators;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Conditions;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Conditions.Consts;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Compilation;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Syntax;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Contexts;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Descriptors;
using Vector2 = System.Numerics.Vector2;
using Session = Celeste.Simulation.Runtime.Session.ChapterCompletionSession;

namespace Celeste.Mods.FrostHelper.Runtime.Expressions.Contexts
{
    using Color = Celeste.Core.Display.PackedColor;

    internal sealed class LambdaContext(IExpressionContext source, LambdaDefinitionCondition lambdaDefinition)
        : IExpressionContext
    {
        public FrostConditionRuntime Runtime => source.Runtime;

        public bool TryGetSimpleCommand(string name, [NotNullWhen(true)] out Condition? command)
        {
            var argIdx = lambdaDefinition.ArgumentNames.IndexOf(name);
            if (argIdx < 0)
            {
                return source.TryGetSimpleCommand(name, out command);
            }

            command = new LambdaArgumentCondition(lambdaDefinition, argIdx);
            return true;
        }

        public bool TryGetFunctionCommand(string name, [NotNullWhen(true)] out FunctionCommand? factory)
        {
            return source.TryGetFunctionCommand(name, out factory);
        }

        public void RegisterSimpleCommand(string name, Condition command)
        {
            throw new Exception("Cannot register commands on a lambda context");
        }

        public void RegisterFunctionCommand(string name, FunctionCommand factory)
        {
            throw new Exception("Cannot register functions on a lambda context");
        }
    }
}
