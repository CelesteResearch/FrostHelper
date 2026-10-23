using System;
using System.Runtime.CompilerServices;

namespace Celeste.Mods.FrostHelper.Runtime.Expressions.Conditions
{
    internal static class ExpressionNumericConversions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int OriginalCastToInt32(float value) =>
            value < 2147483648f && value >= -2147483648f ? (int)value : int.MinValue;

        public static T CreateTruncating<T>(int value) where T : struct
        {
            object converted;
            if (typeof(T) == typeof(int)) converted = value;
            else if (typeof(T) == typeof(float)) converted = (float)value;
            else if (typeof(T) == typeof(double)) converted = (double)value;
            else if (typeof(T) == typeof(long)) converted = (long)value;
            else if (typeof(T) == typeof(uint)) converted = unchecked((uint)value);
            else if (typeof(T) == typeof(ulong)) converted = unchecked((ulong)value);
            else if (typeof(T) == typeof(short)) converted = unchecked((short)value);
            else if (typeof(T) == typeof(ushort)) converted = unchecked((ushort)value);
            else if (typeof(T) == typeof(byte)) converted = unchecked((byte)value);
            else if (typeof(T) == typeof(sbyte)) converted = unchecked((sbyte)value);
            else if (typeof(T) == typeof(decimal)) converted = (decimal)value;
            else return CreateTruncating<T>((double)value);
            return (T)converted;
        }

        public static T CreateTruncating<T>(float value) where T : struct
        {
            if (typeof(T) == typeof(decimal))
            {
                decimal converted = float.IsNaN(value) ? 0m :
                    value >= (float)decimal.MaxValue ? decimal.MaxValue :
                    value <= (float)decimal.MinValue ? decimal.MinValue : (decimal)value;
                return (T)(object)converted;
            }

            return CreateTruncating<T>((double)value);
        }

        public static T CreateTruncating<T>(double value) where T : struct
        {
            object converted;
            if (typeof(T) == typeof(int))
                converted = double.IsNaN(value) ? int.MinValue :
                    value >= int.MaxValue ? int.MaxValue :
                    value <= int.MinValue ? int.MinValue : (int)value;
            else if (typeof(T) == typeof(float)) converted = (float)value;
            else if (typeof(T) == typeof(double)) converted = value;
            else if (typeof(T) == typeof(long))
                converted = double.IsNaN(value) ? 0L :
                    value >= long.MaxValue ? long.MaxValue :
                    value <= long.MinValue ? long.MinValue : (long)value;
            else if (typeof(T) == typeof(uint))
                converted = double.IsNaN(value) || value <= 0 ? 0U :
                    value >= uint.MaxValue ? uint.MaxValue : (uint)value;
            else if (typeof(T) == typeof(ulong))
                converted = double.IsNaN(value) || value <= 0 ? 0UL :
                    value >= ulong.MaxValue ? ulong.MaxValue : (ulong)value;
            else if (typeof(T) == typeof(short))
                converted = (short)(double.IsNaN(value) ? 0 :
                    value >= short.MaxValue ? short.MaxValue :
                    value <= short.MinValue ? short.MinValue : value);
            else if (typeof(T) == typeof(ushort))
                converted = (ushort)(double.IsNaN(value) || value <= 0 ? 0 :
                    value >= ushort.MaxValue ? ushort.MaxValue : value);
            else if (typeof(T) == typeof(byte))
                converted = (byte)(double.IsNaN(value) || value <= 0 ? 0 :
                    value >= byte.MaxValue ? byte.MaxValue : value);
            else if (typeof(T) == typeof(sbyte))
                converted = (sbyte)(double.IsNaN(value) ? 0 :
                    value >= sbyte.MaxValue ? sbyte.MaxValue :
                    value <= sbyte.MinValue ? sbyte.MinValue : value);
            else if (typeof(T) == typeof(decimal))
                converted = double.IsNaN(value) ? 0m :
                    value >= (double)decimal.MaxValue ? decimal.MaxValue :
                    value <= (double)decimal.MinValue ? decimal.MinValue : (decimal)value;
            else throw new ArgumentException($"Unsupported T for Session Expression: {typeof(T).FullName}");
            return (T)converted;
        }
    }
}
