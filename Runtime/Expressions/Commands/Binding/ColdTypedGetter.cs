using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Celeste.Mods.FrostHelper.Runtime.Expressions.Commands.Binding
{
    internal static class ColdTypedGetter<T, TField>
    {
        public static Func<T, TField> ForField(FieldInfo field)
        {
            var method = new DynamicMethod("ReadExpressionTypedField", typeof(TField), new[] { typeof(T) },
                typeof(ColdTypedGetter<T, TField>), true);
            var il = method.GetILGenerator();
            if (typeof(T).IsValueType) il.Emit(OpCodes.Ldarga_S, (byte)0);
            else il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, field);
            il.Emit(OpCodes.Ret);
            return (Func<T, TField>)method.CreateDelegate(typeof(Func<T, TField>));
        }
    }
}
