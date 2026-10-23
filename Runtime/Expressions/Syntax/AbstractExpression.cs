using System.Text.Json;

namespace Celeste.Mods.FrostHelper.Runtime.Expressions.Syntax
{
    internal partial class AbstractExpression
    {
        protected AbstractExpression()
        {
        }

        public string TypeName => GetType().Name;
        public override string ToString() => JsonSerializer.Serialize<object>(this, ExpressionParser.JsonOptions);
    }
}
