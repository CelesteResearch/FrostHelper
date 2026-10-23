namespace Celeste.Mods.FrostHelper.Runtime.Expressions.Compilation
{
    internal static class SessionExpressionExtensions
    {
        extension<T>(SessionExpression<T> expression) where T : struct
        {
            public bool CanBePositive => expression.IsConstant
                ? PositiveValue<T>.IsPositive(expression.ConstantValue)
                : expression.IsNotEmpty;
        }
    }
}
