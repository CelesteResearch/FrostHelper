using System.Numerics;

namespace Celeste.Mods.FrostHelper.Geometry.Triangulation;

internal struct Vertex(Vector2 position, int index)
{
    public readonly Vector2 Position = position;

    public readonly int Index = index;

    public override bool Equals(object obj)
    {
        if (obj == null)
        {
            return false;
        }

        if (obj.GetType() != typeof(Vertex))
        {
            return false;
        }

        return Equals((Vertex)obj);
    }

    public readonly bool Equals(Vertex obj)
    {
        Vector2 position = obj.Position;
        if (position.Equals(Position))
        {
            return obj.Index == Index;
        }

        return false;
    }

    public override int GetHashCode()
    {
        return (((object)Position /*cast due to constrained. prefix*/).GetHashCode() * 397) ^ Index;
    }

    public override string ToString()
    {
        return $"{Position} ({Index})";
    }
}
