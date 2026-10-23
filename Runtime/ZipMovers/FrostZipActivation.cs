using System;

namespace Celeste.Mods.FrostHelper.Runtime.ZipMovers
{
    internal static class FrostZipActivation
    {
        internal static Func<bool> Create(string expression)
        {
            if (string.IsNullOrWhiteSpace(expression)) return static () => true;
            throw new NotSupportedException("FrostHelper session expressions are not supported: " + expression);
        }
    }
}
