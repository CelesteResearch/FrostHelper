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

namespace Celeste.Mods.FrostHelper.Runtime.Expressions.Operators
{
    using Color = Celeste.Core.Display.PackedColor;

    internal sealed class BitwiseOperator<TOp>(Condition condA, Condition condB)
        : BinaryOperator(condA, condB) where TOp : IBitwiseOperator
    {
        private static readonly MethodInfo MethodPerformInt
            = BitwiseOperatorBinding<TOp>.Method;

        protected override bool CoerceMismatchedIntFloat => true;

        public override object Operate(object a, object b)
        {
            return (a, b) switch
            {
                (int aInt, int bInt) => BitwiseOperatorBinding<TOp>.Perform(aInt, bInt),
                (float aF, float bF) => BitwiseOperatorBinding<TOp>.Perform(
                    ExpressionNumericConversions.OriginalCastToInt32(aF),
                    ExpressionNumericConversions.OriginalCastToInt32(bF)),
                _ => LogIncomparableTypes(a, b)
            };
        }

        internal override void Emit(ConditionCompilationCtx ctx, Type targetType)
        {
            if (ConditionA.ReturnTypeIsNumber && ConditionB.ReturnTypeIsNumber)
            {
                EmitGetValuesFromChildConditions(ctx, typeof(int));
                if (BitwiseOperatorBinding<TOp>.OpCode is { } opCode)
                {
                    ctx.Il.Emit(opCode);
                }
                else
                {
                    ctx.Il.Emit(OpCodes.Call, MethodPerformInt);
                }

                ctx.EmitConvertTo(typeof(int), targetType);
            }
            else
            {
                base.Emit(ctx, targetType);
            }
        }

        internal override bool UsesCurrentConditionLocalInEmit
            => InnerConditionsUseCurrentConditionLocalInEmit
               || !ConditionA.ReturnTypeIsNumber
               || !ConditionB.ReturnTypeIsNumber;

        private object LogIncomparableTypes(object a, object b)
        {
            Runtime.Notify(
                $"Can't perform bitwise operations on objects of types: {a.GetType()} and {b.GetType()}. Result will always be 0!");
            return 0;
        }
    }
}
