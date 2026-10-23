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

    internal sealed class MathOperator<TLeft, TRight, TRet, TOp>(Condition condA, Condition condB)
        : BinaryOperator(condA, condB) where TOp : IMathOperator<TLeft, TRight, TRet>
    {
        internal static BinaryOperator Create(Condition left, Condition right)
        {
            return new MathOperator<TLeft, TRight, TRet, TOp>(left, right);
        }

        private static readonly MethodInfo MethodTOpPerform = MathOperatorBinding<TLeft, TRight, TRet, TOp>.Method;

        protected override bool CoerceMismatchedIntFloat => false;

        public override object Operate(object a, object b)
        {
            return MathOperatorBinding<TLeft, TRight, TRet, TOp>.Perform((TLeft)a, (TRight)b)!;
        }

        internal override void Emit(ConditionCompilationCtx ctx, Type targetType)
        {
            if (MathOperatorBinding<TLeft, TRight, TRet, TOp>.OpCode is { } opCode &&
                MathOperatorBinding<TLeft, TRight, TRet, TOp>.CanUseOpCodeFor(ConditionA, ConditionB))
            {
                if ((typeof(TLeft) == typeof(int) && typeof(TRight) == typeof(float))
                    || (typeof(TLeft) == typeof(float) && typeof(TRight) == typeof(int))
                    || (typeof(TOp) == typeof(OperatorDivFloat) && typeof(TLeft) == typeof(int) &&
                        typeof(TRight) == typeof(int)))
                {
                    EmitGetValuesFromChildConditions(ctx, typeof(float), typeof(float));
                }
                else
                {
                    EmitGetValuesFromChildConditions(ctx, typeof(TLeft), typeof(TRight));
                }

                ctx.Il.Emit(opCode);
                ctx.Il.EmitConvertToInSessionExpression(ReturnType, targetType);
                return;
            }

            EmitGetValuesFromChildConditions(ctx, typeof(TLeft), typeof(TRight));
            ctx.Il.Emit(OpCodes.Call, MethodTOpPerform);
            ctx.Il.EmitConvertToInSessionExpression(ReturnType, targetType);
        }

        internal override bool UsesCurrentConditionLocalInEmit => InnerConditionsUseCurrentConditionLocalInEmit;

        protected internal override Type ReturnType { get; } = typeof(TRet);
    }
}
