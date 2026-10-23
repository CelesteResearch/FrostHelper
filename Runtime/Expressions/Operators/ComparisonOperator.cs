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

    internal sealed class ComparisonOperator<TOp>(Condition condA, Condition condB)
        : BinaryOperator(condA, condB) where TOp : IComparisonOperator
    {
        private static readonly MethodInfo Method_TOp_Compare_String_String =
            ComparisonOperatorBinding<string, TOp>.Method;

        private static readonly MethodInfo Method_Dispatch = typeof(ComparisonOperator<TOp>)
            .GetMethod(nameof(Dispatch), BindingFlags.Static | BindingFlags.NonPublic, null,
                [typeof(ComparisonOperator<TOp>), typeof(object), typeof(object)], null)!;


        private static bool Dispatch(ComparisonOperator<TOp> owner, object a, object b)
        {
            return (a, b) switch
            {
                (int ai, int bi) => ComparisonOperatorBinding<int, TOp>.Compare(ai, bi),
                (float ai, float bi) => ComparisonOperatorBinding<float, TOp>.Compare(ai, bi),
                (string ai, string bi) => ComparisonOperatorBinding<string, TOp>.Compare(ai, bi),
                _ => owner.LogIncomparableTypes(a, b)
            };
        }

        protected override bool CoerceMismatchedIntFloat => true;

        public override object Operate(object a, object b)
        {
            return Dispatch(this, a, b) ? One : Zero;
        }

        internal override void Emit(ConditionCompilationCtx ctx, Type targetType)
        {
            var aType = ConditionA.ReturnType;
            var bType = ConditionB.ReturnType;

            if (ConditionA.ReturnTypeIsNumber && ConditionB.ReturnTypeIsNumber)
            {
                var coercedType = aType == bType ? aType! : typeof(float);

                EmitGetValuesFromChildConditions(ctx, coercedType);
                if (ComparisonOperatorBinding<int, TOp>.OpCodeSequence is { } opCodes)
                {
                    foreach (var opCode in opCodes)
                    {
                        ctx.Il.Emit(opCode);
                    }
                }
                else
                {
                    ctx.Il.Emit(OpCodes.Call,
                        coercedType == typeof(int)
                            ? ComparisonOperatorBinding<int, TOp>.Method
                            : ComparisonOperatorBinding<float, TOp>.Method);
                }

                ctx.EmitConvertTo(typeof(bool), targetType);
                return;
            }

            if (aType == typeof(string) && bType == typeof(string))
            {
                EmitGetValuesFromChildConditions(ctx, typeof(string));
                ctx.Il.Emit(OpCodes.Call, Method_TOp_Compare_String_String);
                ctx.EmitConvertTo(typeof(bool), targetType);
                return;
            }


            ctx.EmitLoadCurrentCondition<ComparisonOperator<TOp>>();
            EmitGetValuesFromChildConditions(ctx, typeof(object));
            ctx.Il.Emit(OpCodes.Call, Method_Dispatch);
            ctx.EmitConvertTo(typeof(bool), targetType);
        }

        internal override bool UsesCurrentConditionLocalInEmit => InnerConditionsUseCurrentConditionLocalInEmit ||
                                                                  !(ConditionA.ReturnTypeIsNumber &&
                                                                    ConditionB.ReturnTypeIsNumber) &&
                                                                  !(ConditionA.ReturnType == typeof(string) &&
                                                                    ConditionB.ReturnType == typeof(string));

        private bool LogIncomparableTypes(object a, object b)
        {
            Runtime.Notify(
                $"Can't compare objects of types: {a.GetType()} and {b.GetType()}. Result will always be 0!");
            return false;
        }

        protected internal override Type ReturnType => typeof(int);
    }
}
