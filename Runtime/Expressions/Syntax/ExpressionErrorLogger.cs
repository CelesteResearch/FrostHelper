using System;

namespace Celeste.Mods.FrostHelper.Runtime.Expressions.Syntax
{
    internal sealed class ExpressionErrorLogger(Action<string> notify) : IAbstractExpressionErrorLogger
    {
        public void Error(string message) => notify(message);
    }
}
