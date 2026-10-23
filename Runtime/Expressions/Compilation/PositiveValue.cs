using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Celeste.Mods.FrostHelper.Runtime.Expressions.Compilation
{
    internal static class PositiveValue<T>
    {
        private static Func<T, bool>? predicate;
        internal static bool IsPositive(T value) => (predicate ??= Bind())(value);

        internal static void PrepareIfNumeric()
        {
            var type = typeof(T);
            if (IsBuiltin(type) || type.GetInterfaces().Any(i =>
                                    i.IsGenericType &&
                                    i.GetGenericTypeDefinition().FullName == "System.Numerics.INumber`1" &&
                                    i.GetGenericArguments()[0] == type)
                                || FindOperator(type) != null &&
                                (FindZeroGetter(type) != null || FindZeroField(type) != null))
                predicate ??= Bind();
        }

        private static bool IsBuiltin(Type type) =>
            type == typeof(int) || type == typeof(float) || type == typeof(double)
            || type == typeof(long) || type == typeof(uint) || type == typeof(ulong) || type == typeof(short)
            || type == typeof(ushort) || type == typeof(byte) || type == typeof(sbyte) || type == typeof(IntPtr) ||
            type == typeof(UIntPtr);

        private static MethodInfo? FindOperator(Type type) => type
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .FirstOrDefault(m =>
                (m.Name == "op_GreaterThan" || m.Name.EndsWith(".op_GreaterThan", StringComparison.Ordinal)) &&
                m.ReturnType == typeof(bool)
                && !m.IsGenericMethodDefinition && m.GetParameters().Length == 2 &&
                m.GetParameters()[0].ParameterType == type && m.GetParameters()[1].ParameterType == type);

        private static MethodInfo? FindZeroGetter(Type type) => type
            .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static)
            .FirstOrDefault(m =>
                (m.Name == "get_Zero" || m.Name.EndsWith(".get_Zero", StringComparison.Ordinal)) &&
                m.ReturnType == type && m.GetParameters().Length == 0);

        private static FieldInfo? FindZeroField(Type type)
        {
            var field = type.GetField("Zero", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            return field?.FieldType == type ? field : null;
        }

        private static MethodInfo FindInterfaceMember(Type type, string name, Type result, int count) => type
            .GetInterfaces()
            .SelectMany(i => i.GetMethods(BindingFlags.Public | BindingFlags.Static))
            .First(m => m.Name == name && m.ReturnType == result && m.GetParameters().Length == count &&
                        (count == 0 || m.GetParameters().All(p => p.ParameterType == type)));

        private static Func<T, bool> Bind()
        {
            var type = typeof(T);
            var method = new DynamicMethod("IsPositive", typeof(bool), [type], typeof(PositiveValue<T>), true);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            if (IsBuiltin(type))
            {
                if (type == typeof(float)) il.Emit(OpCodes.Ldc_R4, 0f);
                else if (type == typeof(double)) il.Emit(OpCodes.Ldc_R8, 0d);
                else
                {
                    il.Emit(OpCodes.Ldc_I4_0);
                    if (type == typeof(long) || type == typeof(ulong)) il.Emit(OpCodes.Conv_I8);
                    else if (type == typeof(IntPtr)) il.Emit(OpCodes.Conv_I);
                    else if (type == typeof(UIntPtr)) il.Emit(OpCodes.Conv_U);
                }

                il.Emit(type == typeof(uint) || type == typeof(ulong) || type == typeof(ushort) ||
                        type == typeof(byte) || type == typeof(UIntPtr)
                    ? OpCodes.Cgt_Un
                    : OpCodes.Cgt);
            }
            else
            {
                var zeroGetter = FindZeroGetter(type);
                var zeroField = FindZeroField(type);
                if (zeroGetter != null) il.Emit(OpCodes.Call, zeroGetter);
                else if (zeroField != null) il.Emit(OpCodes.Ldsfld, zeroField);
                else
                {
                    zeroGetter = FindInterfaceMember(type, "get_Zero", type, 0);
                    il.Emit(OpCodes.Constrained, type);
                    il.Emit(OpCodes.Call, zeroGetter);
                }

                var comparison = FindOperator(type);
                if (comparison == null)
                {
                    comparison = FindInterfaceMember(type, "op_GreaterThan", typeof(bool), 2);
                    il.Emit(OpCodes.Constrained, type);
                }

                il.Emit(OpCodes.Call, comparison);
            }

            il.Emit(OpCodes.Ret);
            return (Func<T, bool>)method.CreateDelegate(typeof(Func<T, bool>));
        }
    }
}
