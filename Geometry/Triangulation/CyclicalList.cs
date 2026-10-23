using System.Collections.Generic;

namespace Celeste.Mods.FrostHelper.Geometry.Triangulation;

internal class CyclicalList<T> : List<T>
{
    public new T this[int index]
    {
        get
        {
            while (index < 0)
            {
                index = base.Count + index;
            }

            if (index >= base.Count)
            {
                index %= base.Count;
            }

            return base[index];
        }
        set
        {
            while (index < 0)
            {
                index = base.Count + index;
            }

            if (index >= base.Count)
            {
                index %= base.Count;
            }

            base[index] = value;
        }
    }

    public CyclicalList()
    {
    }

    public CyclicalList(IEnumerable<T> collection)
        : base(collection)
    {
    }

    public new void RemoveAt(int index)
    {
        Remove(this[index]);
    }
}
