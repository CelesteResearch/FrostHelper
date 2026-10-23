using System;
using System.Globalization;
using System.Numerics;

namespace Celeste.Mods.FrostHelper.Runtime.Expressions.Operators
{
    internal sealed partial class ExpressionInterpolator
    {
        internal ref struct Handler
        {
            public IFormatProvider FormatProvider { get; set; }
            private int length;
            private char[] data;

            public Handler(int literalLength, int formattedCount, ExpressionInterpolator interpolator)
            {
                length = 0;
                FormatProvider = CultureInfo.InvariantCulture;
                data = interpolator.buffer;
            }

            private int CapacityLeft => data.Length - length;
            private Span<char> RemainingSpan() => data.AsSpan(length);
            private void Expand(int newSize) => Array.Resize(ref data, newSize);
            public string ResultToString() => data.AsSpan(0, length).ToString();

            public void AppendLiteral(string? value)
            {
                var span = value.AsSpan();
                if (span.Length > CapacityLeft)
                    Expand(data.Length + span.Length);
                span.CopyTo(RemainingSpan());
                length += span.Length;
            }

            public void AppendFormatted(Vector2 value)
            {
                AppendLiteral("{X:");
                AppendVectorComponent(value.X);
                AppendLiteral(" Y:");
                AppendVectorComponent(value.Y);
                AppendLiteral("}");
            }

            private void AppendVectorComponent(float value)
            {
                int written;
                while (!value.TryFormat(RemainingSpan(), out written, "G6", CultureInfo.CurrentCulture))
                    Expand(data.Length * 2);
                length += written;
            }

            public void AppendFormatted(object value)
            {
                if (value is Vector2 vector)
                {
                    AppendFormatted(vector);
                    return;
                }

                if (value is IFormattable formattable)
                {
                    AppendLiteral(formattable.ToString(null, FormatProvider));
                    return;
                }

                AppendLiteral(value.ToString());
            }

            public void AppendFormatted<T>(T value)
            {
                int written;
                while (!SpanFormatting<T>.TryFormat(value, RemainingSpan(), out written, "", FormatProvider))
                    Expand(data.Length * 2);
                length += written;
            }
        }
    }
}
