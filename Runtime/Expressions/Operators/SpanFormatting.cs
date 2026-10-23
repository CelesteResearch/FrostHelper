using System;
using System.Reflection.Emit;

namespace Celeste.Mods.FrostHelper.Runtime.Expressions.Operators
{
    internal static class SpanFormatting<T>
    {
        internal static readonly SpanFormatDelegate<T> TryFormat = Bind();

        private static SpanFormatDelegate<T> Bind()
        {
            var target = SpanFormattingSupport.GetMethod(typeof(T));
            var method = new DynamicMethod("FormatSpan", typeof(bool),
            [
                typeof(T), typeof(Span<char>), typeof(int).MakeByRefType(), typeof(ReadOnlySpan<char>),
                typeof(IFormatProvider)
            ], typeof(SpanFormatting<T>), true);
            var il = method.GetILGenerator();
            if (typeof(T).IsValueType)
                il.Emit(OpCodes.Ldarga_S, (byte)0);
            else
                il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Ldarg_3);
            if (target.GetParameters().Length == 4)
                il.Emit(OpCodes.Ldarg_S, (byte)4);
            if (target.DeclaringType!.IsInterface)
            {
                if (typeof(T).IsValueType)
                    il.Emit(OpCodes.Constrained, typeof(T));
                il.Emit(OpCodes.Callvirt, target);
            }
            else
                il.Emit(typeof(T).IsValueType ? OpCodes.Call : OpCodes.Callvirt, target);

            il.Emit(OpCodes.Ret);
            return (SpanFormatDelegate<T>)method.CreateDelegate(typeof(SpanFormatDelegate<T>));
        }
    }
}
