using System;
using System.Runtime.CompilerServices;

namespace Celeste.Mods.FrostHelper.Runtime.Expressions.Syntax
{
    internal ref struct SpanParser(ReadOnlySpan<char> input)
    {
        private ReadOnlySpan<char> _remaining = input;


        public ReadOnlySpan<char> Remaining => _remaining;


        public bool IsEmpty => _remaining.IsEmpty;


        private ReadOnlySpan<char> ReadSliceStr(int len)
        {
            var ret = _remaining[..len];
            _remaining = _remaining[len..];

            return ret;
        }


        /// <summary>
        /// Reads the remaining span to completion, returning that span.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<char> ReadStr()
            => ReadSliceStr(_remaining.Length);


        /// <summary>
        /// Reads the next 'len' chars, returning that span.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ReadOnlySpan<char> ReadStr(int len)
            => ReadSliceStr(len);


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool StartsWith(ReadOnlySpan<char> prefix)
            => _remaining.StartsWith(prefix);


        public void TrimStart()
        {
            _remaining = _remaining.TrimStart();
        }


        public void TrimStart(out string trimmed)
        {
            var ret = _remaining.TrimStart();
            if (_remaining.Length == ret.Length)
            {
                trimmed = string.Empty;
                return;
            }

            trimmed = _remaining[..^ret.Length].ToString();
            _remaining = ret;
        }


        public bool TryTrimPrefix(ReadOnlySpan<char> prefix)
        {
            if (!StartsWith(prefix))
                return false;

            Skip(prefix.Length);
            return true;
        }


        public void Skip(int chars)
        {
            _remaining = _remaining[chars..];
        }
    }
}
