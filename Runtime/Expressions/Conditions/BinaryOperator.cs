using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Compilation;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Descriptors;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Conditions.Consts;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Operators;
using Session = Celeste.Simulation.Runtime.Session.ChapterCompletionSession;

namespace Celeste.Mods.FrostHelper.Runtime.Expressions.Conditions
{
    using Color = Celeste.Core.Display.PackedColor;


    internal abstract class BinaryOperator(Condition condA, Condition condB) : Condition
    {
        protected readonly Condition ConditionA = condA;
        protected readonly Condition ConditionB = condB;

        protected abstract bool CoerceMismatchedIntFloat { get; }

        public override object Get(Session session, object? userdata)
        {
            var a = ConditionA.Get(session, userdata);
            var b = ConditionB.Get(session, userdata);

            if (a is bool ab)
                a = ab ? One : Zero;
            if (b is bool bb)
                a = bb ? One : Zero;

            if (!CoerceMismatchedIntFloat)
                return Operate(a, b);

            return (a, b) switch
            {
                // When floats and ints are mismatched, perform operation on floats
                (int ai, float bi) => Operate((float)ai, (float)bi),
                (float ai, int bi) => Operate((float)ai, (float)bi),
                _ => Operate(a, b)
            };
        }

        protected void EmitGetValuesFromChildConditions(ConditionCompilationCtx ctx, Type targetType,
            Type? targetTypeB = null)
        {
            var il = ctx.Il;
            LocalBuilder? tempOrigCond = null;
            targetTypeB ??= targetType;

            il.EmitSwapOutCurrentCondition(ref tempOrigCond, ctx, ConditionA,
                typeof(BinaryOperator).GetField(nameof(ConditionA), BindingFlags.Instance | BindingFlags.NonPublic)!);
            ConditionA.Emit(ctx, targetType);
            il.EmitSwapOutCurrentCondition(ref tempOrigCond, ctx, ConditionB,
                typeof(BinaryOperator).GetField(nameof(ConditionB), BindingFlags.Instance | BindingFlags.NonPublic)!);
            ConditionB.Emit(ctx, targetTypeB);

            il.EmitRevertCurrentCondition(tempOrigCond, ctx);
        }

        protected bool InnerConditionsUseCurrentConditionLocalInEmit => ConditionA.UsesCurrentConditionLocalInEmit ||
                                                                        ConditionB.UsesCurrentConditionLocalInEmit;

        public override bool OnlyChecksFlags() => ConditionA.OnlyChecksFlags() && ConditionB.OnlyChecksFlags();

        public abstract object Operate(object a, object b);

        protected override IEnumerable<object> GetArgsForDebugPrint() => [ConditionA, ConditionB];
    }
}
