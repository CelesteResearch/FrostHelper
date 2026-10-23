using System;

namespace Celeste.Mods.FrostHelper.Runtime.Expressions.Operators
{
    internal delegate bool SpanFormatDelegate<T>(T value, Span<char> destination, out int written,
        ReadOnlySpan<char> format, IFormatProvider provider);
}
