using System;
using System.Reflection;

namespace Celeste.Mods.FrostHelper.Runtime.Expressions.Commands.Binding
{
    internal static class FieldOperation<T, TField, TImpl>
    {
        public static readonly Func<T, TField> Invoke =
            (Func<T, TField>)typeof(TImpl).GetMethod("GetValue", BindingFlags.Public | BindingFlags.Static)!
                .CreateDelegate(typeof(Func<T, TField>));
    }
}
