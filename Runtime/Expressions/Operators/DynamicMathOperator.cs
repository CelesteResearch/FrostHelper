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

    internal sealed class DynamicMathOperator(
        FrostConditionRuntime runtime,
        BinOpExpression.Operators op,
        Condition condA,
        Condition condB)
        : BinaryOperator(condA, condB)
    {
        protected override bool CoerceMismatchedIntFloat => false;

        public override object Operate(object a, object b)
        {
            var cond = runtime.MathOperators.CreateFor(op, ConditionA, ConditionB, a.GetType(), b.GetType());

            return cond.Operate(a, b);
        }
    }
}
