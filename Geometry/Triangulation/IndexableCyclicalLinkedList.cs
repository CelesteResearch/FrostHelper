using System.Collections.Generic;

namespace Celeste.Mods.FrostHelper.Geometry.Triangulation;

internal class IndexableCyclicalLinkedList<T> : LinkedList<T>
{
    public LinkedListNode<T> this[int index]
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

            LinkedListNode<T> linkedListNode = base.First;
            for (int i = 0; i < index; i++)
            {
                linkedListNode = linkedListNode.Next;
            }

            return linkedListNode;
        }
    }

    public void RemoveAt(int index)
    {
        Remove(this[index]);
    }

    public int IndexOf(T item)
    {
        for (int i = 0; i < base.Count; i++)
        {
            if (this[i].Value.Equals(item))
            {
                return i;
            }
        }

        return -1;
    }
}
