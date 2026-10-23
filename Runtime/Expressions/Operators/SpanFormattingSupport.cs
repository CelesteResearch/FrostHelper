using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Celeste.Mods.FrostHelper.Runtime.Expressions.Operators
{
    internal static class SpanFormattingSupport
    {
        internal static void Prepare(Type type) =>
            RuntimeHelpers.RunClassConstructor(typeof(SpanFormatting<>).MakeGenericType(type).TypeHandle);

        internal static Type? GetContract(Type type) => type.GetInterface("System.ISpanFormattable");

        internal static bool Supports(Type type)
        {
            if (GetContract(type) != null)
                return true;
            // These BCL value types expose the identical native span contract on netstandard2.1 hosts.
            return type == typeof(int) || type == typeof(float) || type == typeof(double)
                   || type == typeof(long) || type == typeof(uint) || type == typeof(ulong)
                   || type == typeof(short) || type == typeof(ushort) || type == typeof(byte)
                   || type == typeof(sbyte) || type == typeof(decimal) || type == typeof(DateTime)
                   || type == typeof(DateTimeOffset) || type == typeof(TimeSpan) || type == typeof(Guid);
        }

        internal static MethodInfo GetMethod(Type type)
        {
            var signature = new[]
            {
                typeof(Span<char>), typeof(int).MakeByRefType(), typeof(ReadOnlySpan<char>), typeof(IFormatProvider)
            };
            return type.GetMethod("TryFormat", BindingFlags.Public | BindingFlags.Instance, null, signature, null)
                   ?? type.GetMethod("TryFormat", BindingFlags.Public | BindingFlags.Instance, null,
                       [typeof(Span<char>), typeof(int).MakeByRefType(), typeof(ReadOnlySpan<char>)], null)
                   ?? GetContract(type)!.GetMethod("TryFormat", signature)!;
        }
    }
}
