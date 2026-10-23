using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace Celeste.Mods.FrostHelper.Runtime.Expressions.Operators
{
    internal static class ComparisonOperatorBinding<TValue, TOperator>
    {
        internal static readonly MethodInfo Method = typeof(TOperator).GetMethod("Compare",
            BindingFlags.Static | BindingFlags.Public, null, [typeof(TValue), typeof(TValue)], null)!;

        internal static readonly Func<TValue, TValue, bool> Compare =
            (Func<TValue, TValue, bool>)Method.CreateDelegate(typeof(Func<TValue, TValue, bool>));

        internal static readonly List<OpCode>? OpCodeSequence =
            (List<OpCode>?)typeof(TOperator).GetProperty("OpCodeSequence", BindingFlags.Static | BindingFlags.Public)!
                .GetValue(null);
    }
}
