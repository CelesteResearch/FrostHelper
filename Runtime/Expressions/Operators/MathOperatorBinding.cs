using System;
using System.Reflection;
using System.Reflection.Emit;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Conditions;

namespace Celeste.Mods.FrostHelper.Runtime.Expressions.Operators
{
    internal static class MathOperatorBinding<TLeft, TRight, TResult, TOperator>
    {
        internal static readonly MethodInfo Method = typeof(TOperator).GetMethod("Perform",
            BindingFlags.Static | BindingFlags.Public, null, [typeof(TLeft), typeof(TRight)], null)!;

        internal static readonly Func<TLeft, TRight, TResult> Perform =
            (Func<TLeft, TRight, TResult>)Method.CreateDelegate(typeof(Func<TLeft, TRight, TResult>));

        internal static readonly OpCode? OpCode =
            (OpCode?)typeof(TOperator).GetProperty("PerformOpCode", BindingFlags.Static | BindingFlags.Public)!
                .GetValue(null);

        internal static readonly Func<Condition, Condition, bool> CanUseOpCodeFor =
            (Func<Condition, Condition, bool>)typeof(TOperator).GetMethod("CanUseOpCodeFor",
                BindingFlags.Static | BindingFlags.Public)!.CreateDelegate(typeof(Func<Condition, Condition, bool>));
    }
}
