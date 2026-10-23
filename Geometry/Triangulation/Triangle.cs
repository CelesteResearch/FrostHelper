namespace Celeste.Mods.FrostHelper.Geometry.Triangulation;

internal struct Triangle(Vertex a, Vertex b, Vertex c)
{
    public readonly Vertex A = a;

    public readonly Vertex B = b;

    public readonly Vertex C = c;

    public bool ContainsPoint(Vertex point)
    {
        if (point.Equals(A) || point.Equals(B) || point.Equals(C))
        {
            return true;
        }

        bool flag = false;
        if (checkPointToSegment(C, A, point))
        {
            flag = !flag;
        }

        if (checkPointToSegment(A, B, point))
        {
            flag = !flag;
        }

        if (checkPointToSegment(B, C, point))
        {
            flag = !flag;
        }

        return flag;
    }

    public static bool ContainsPoint(Vertex a, Vertex b, Vertex c, Vertex point)
    {
        return new Triangle(a, b, c).ContainsPoint(point);
    }

    private static bool checkPointToSegment(Vertex sA, Vertex sB, Vertex point)
    {
        if (((sA.Position.Y < point.Position.Y && sB.Position.Y >= point.Position.Y) ||
             (sB.Position.Y < point.Position.Y && sA.Position.Y >= point.Position.Y)) &&
            sA.Position.X + (point.Position.Y - sA.Position.Y) / (sB.Position.Y - sA.Position.Y) *
            (sB.Position.X - sA.Position.X) < point.Position.X)
        {
            return true;
        }

        return false;
    }

    public override bool Equals(object obj)
    {
        if (obj == null)
        {
            return false;
        }

        if (obj.GetType() != typeof(Triangle))
        {
            return false;
        }

        return Equals((Triangle)obj);
    }

    public bool Equals(Triangle obj)
    {
        if (obj.A.Equals(A) && obj.B.Equals(B))
        {
            return obj.C.Equals(C);
        }

        return false;
    }

    public override int GetHashCode()
    {
        return (((A.GetHashCode() * 397) ^ B.GetHashCode()) * 397) ^ C.GetHashCode();
    }
}
