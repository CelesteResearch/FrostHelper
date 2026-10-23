using System;

namespace Celeste.Mods.FrostHelper.Runtime.Expressions.Operators
{
    internal sealed partial class ExpressionInterpolator
    {
        [ThreadStatic] private static ExpressionInterpolator? shared;
        private readonly char[] buffer = new char[128];
        public static ExpressionInterpolator Shared => shared ??= new ExpressionInterpolator();
    }
}
