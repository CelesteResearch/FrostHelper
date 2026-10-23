using System;
using System.Collections.Generic;
using Celeste.Core.Mathematics.Easing;

namespace Celeste.Mods.FrostHelper.Animation.Easing
{
    public sealed class FrostEasingResolver
    {
        private readonly ISourceEasingArithmetic arithmetic;
        private readonly Dictionary<string, Func<float, float>> easers = new(StringComparer.OrdinalIgnoreCase);
        private readonly Func<string, Func<float, float>> luaLoader;
        private readonly Func<string, Func<float, float>> expressionLoader;
        private readonly Action<string> warning;

        public FrostEasingResolver(ISourceEasingArithmetic arithmetic,
            Func<string, Func<float, float>> luaLoader = null,
            Func<string, Func<float, float>> expressionLoader = null, Action<string> warning = null)
        {
            this.arithmetic = arithmetic ?? throw new ArgumentNullException(nameof(arithmetic));
            this.luaLoader = luaLoader;
            this.expressionLoader = expressionLoader;
            this.warning = warning;
            if (arithmetic is ISourceNamedEasingArithmetic named)
                foreach (var entry in named.NamedEasers)
                    easers.Add(entry.Key, entry.Value);
            else
                foreach (var property in typeof(ISourceEasingArithmetic).GetProperties())
                    if (property.PropertyType == typeof(Func<float, float>))
                        easers.Add(property.Name, (Func<float, float>)property.GetValue(arithmetic));
            // Native Frost Lua compiles an empty expression to a nil return, converted to zero.
            if (luaLoader == null) easers.Add(string.Empty, static _ => 0f);
        }

        public Func<float, float> Resolve(string name)
        {
            if (name == null) throw new ArgumentNullException(nameof(name));
            if (easers.TryGetValue(name, out var cached)) return cached ?? arithmetic.Linear;
            bool expression = name.StartsWith("expr:", StringComparison.Ordinal);
            var loader = expression ? expressionLoader : luaLoader;
            if (loader == null)
                throw new NotSupportedException(expression
                    ? "Frost session-expression easing requires its real ConditionHelper integration."
                    : "Unknown or unavailable named easing '" + name +
                      "' requires the real Frost Lua easing integration.");
            var result = loader(expression ? name.Substring(5) : name);
            easers[name] = result;
            if (result == null) warning?.Invoke("Failed to load Frost easer '" + name + "'.");
            // A genuine loader failure follows Frost's cached-null, default-Linear behavior.
            return result ?? arithmetic.Linear;
        }
    }
}
