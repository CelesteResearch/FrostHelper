using System;
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

namespace Celeste.Mods.FrostHelper.Runtime.Expressions.Conditions
{
    using Color = Celeste.Core.Display.PackedColor;


    internal sealed class LambdaArgumentCondition(LambdaDefinitionCondition lambdaDefinition, int index) : Condition
    {
        private static readonly MethodInfo MethodGetArgument =
            typeof(LambdaArgumentCondition).GetMethod(nameof(GetArgument),
                BindingFlags.NonPublic | BindingFlags.Instance)!;

        private object GetArgument()
        {
            return lambdaDefinition.Instance?.GetArgument(index) ?? Zero;
        }

        public override object Get(Session session, object? userdata)
        {
            return GetArgument();
        }

        internal override void Emit(ConditionCompilationCtx ctx, Type targetType)
        {
            ctx.EmitLoadCurrentCondition<LambdaArgumentCondition>();
            ctx.Il.Emit(OpCodes.Call, MethodGetArgument);
            if (ReturnType.IsValueType)
            {
                ctx.Il.Emit(OpCodes.Unbox_Any, ReturnType);
            }

            ctx.EmitConvertTo(ReturnType, targetType);
        }

        protected internal override Type ReturnType => lambdaDefinition.GetArgumentType(index);
    }
}
