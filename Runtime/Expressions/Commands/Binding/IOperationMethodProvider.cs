using System.Reflection;

namespace Celeste.Mods.FrostHelper.Runtime.Expressions.Commands.Binding
{
    internal interface IOperationMethodProvider
    {
        MethodInfo ImplementationMethod { get; }
    }
}
