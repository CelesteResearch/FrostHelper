using System;
using System.Reflection;

namespace Celeste.Mods.FrostHelper.Runtime.Expressions.Commands.Binding
{
    internal static class OperationMetadata<TOp>
    {
        public static readonly MethodInfo GetMethod = typeof(IOperationMethodProvider).IsAssignableFrom(typeof(TOp))
            ? ((IOperationMethodProvider)Activator.CreateInstance(typeof(TOp))!).ImplementationMethod
            : typeof(TOp).GetMethod("Get", BindingFlags.Public | BindingFlags.Static)!;

        public static string GetArgumentName(string name) =>
            (string)typeof(TOp).GetProperty(name, BindingFlags.Public | BindingFlags.Static)!.GetValue(null)!;
    }
}
