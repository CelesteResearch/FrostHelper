using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Celeste.Mods.FrostHelper.Runtime.Expressions.Commands.Binding
{
    internal static class ColdObjectGetter
    {
        public static Func<object, object> ForField(FieldInfo field)
        {
            var method = new DynamicMethod("ReadExpressionField", typeof(object), new[] { typeof(object) },
                typeof(ColdObjectGetter), true);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Castclass, field.DeclaringType!);
            il.Emit(OpCodes.Ldfld, field);
            if (field.FieldType.IsValueType) il.Emit(OpCodes.Box, field.FieldType);
            il.Emit(OpCodes.Ret);
            return (Func<object, object>)method.CreateDelegate(typeof(Func<object, object>));
        }

        public static Func<object, object> ForProperty(PropertyInfo property)
        {
            var method = new DynamicMethod("ReadExpressionProperty", typeof(object), new[] { typeof(object) },
                typeof(ColdObjectGetter), true);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Castclass, property.DeclaringType!);
            il.Emit(OpCodes.Callvirt, property.GetMethod!);
            if (property.PropertyType.IsValueType) il.Emit(OpCodes.Box, property.PropertyType);
            il.Emit(OpCodes.Ret);
            return (Func<object, object>)method.CreateDelegate(typeof(Func<object, object>));
        }
    }
}
