using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Celeste.Mods.FrostHelper.Runtime.Expressions.Operators
{
    internal static class BitwiseOperatorBinding<TOperator>
    {
        internal static readonly MethodInfo Method = typeof(TOperator).GetMethod("Perform",
            BindingFlags.Static | BindingFlags.Public, null, [typeof(int), typeof(int)], null)!;

        internal static readonly Func<int, int, int> Perform =
            (Func<int, int, int>)Method.CreateDelegate(typeof(Func<int, int, int>));

        internal static readonly OpCode? OpCode =
            (OpCode?)typeof(TOperator).GetProperty("OpCode", BindingFlags.Static | BindingFlags.Public)!.GetValue(null);
    }
}
