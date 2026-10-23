// SPDX-License-Identifier: MIT

using System;

namespace Celeste.Mods.FrostHelper.Entities.Decals
{
    internal sealed class DecalInfoBuffer
    {
        private DecalInfo[] items = Array.Empty<DecalInfo>();
        internal int Count { get; private set; }
        internal ReadOnlySpan<DecalInfo> AsSpan() => new(items, 0, Count);

        internal void Add(DecalInfo item)
        {
            if (Count == items.Length)
            {
                int capacity = items.Length == 0 ? 4 : items.Length * 2;
                if ((uint)capacity > 0X7FFFFFC7) capacity = 0X7FFFFFC7;
                if (capacity < Count + 1) capacity = Count + 1;
                Array.Resize(ref items, capacity);
            }

            items[Count++] = item;
        }

        internal bool Remove(DecalInfo item)
        {
            int index = Array.IndexOf(items, item, 0, Count);
            if (index < 0) return false;
            Count--;
            if (index < Count) Array.Copy(items, index + 1, items, index, Count - index);
            items[Count] = null;
            return true;
        }
    }
}
