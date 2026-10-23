using System;
using System.Collections.Generic;
using System.Numerics;

namespace Celeste.Mods.FrostHelper.Geometry.Triangulation;

public sealed class ShapeTriangulator
{
    private readonly IndexableCyclicalLinkedList<Vertex> polygonVertices = new IndexableCyclicalLinkedList<Vertex>();

    private readonly IndexableCyclicalLinkedList<Vertex> earVertices = new IndexableCyclicalLinkedList<Vertex>();

    private readonly CyclicalList<Vertex> convexVertices = new CyclicalList<Vertex>();

    private readonly CyclicalList<Vertex> reflexVertices = new CyclicalList<Vertex>();

    public void Triangulate(Vector2[] inputVertices, WindingOrder desiredWindingOrder, WindingOrder? inputWindingOrder,
        out Vector2[] outputVertices, out int[] indices)
    {
        List<Triangle> list = new List<Triangle>();
        WindingOrder valueOrDefault = inputWindingOrder.GetValueOrDefault();
        if (!inputWindingOrder.HasValue)
        {
            valueOrDefault = DetermineWindingOrder(inputVertices);
            inputWindingOrder = valueOrDefault;
        }

        outputVertices = inputWindingOrder switch
        {
            WindingOrder.Clockwise => ReverseWindingOrder(inputVertices),
            WindingOrder.CounterClockwise => (Vector2[])inputVertices.Clone(),
            _ => throw new NotImplementedException(),
        };
        polygonVertices.Clear();
        earVertices.Clear();
        convexVertices.Clear();
        reflexVertices.Clear();
        for (int i = 0; i < outputVertices.Length; i++)
        {
            polygonVertices.AddLast(new Vertex(outputVertices[i], i));
        }

        FindConvexAndReflexVertices();
        FindEarVertices();
        while (polygonVertices.Count > 3 && earVertices.Count > 0)
        {
            ClipNextEar(list);
        }

        if (polygonVertices.Count == 3)
        {
            list.Add(new Triangle(polygonVertices[0].Value, polygonVertices[1].Value, polygonVertices[2].Value));
        }

        indices = new int[list.Count * 3];
        if (desiredWindingOrder == WindingOrder.CounterClockwise)
        {
            for (int j = 0; j < list.Count; j++)
            {
                indices[j * 3] = list[j].A.Index;
                indices[j * 3 + 1] = list[j].B.Index;
                indices[j * 3 + 2] = list[j].C.Index;
            }
        }
        else
        {
            for (int k = 0; k < list.Count; k++)
            {
                indices[k * 3] = list[k].C.Index;
                indices[k * 3 + 1] = list[k].B.Index;
                indices[k * 3 + 2] = list[k].A.Index;
            }
        }
    }

    public static Vector2[] EnsureWindingOrder(Vector2[] vertices, WindingOrder windingOrder)
    {
        if (DetermineWindingOrder(vertices) != windingOrder)
        {
            return ReverseWindingOrder(vertices);
        }

        return vertices;
    }

    public static Vector2[] ReverseWindingOrder(Vector2[] vertices)
    {
        Vector2[] array = (Vector2[])(object)new Vector2[vertices.Length];
        array[0] = vertices[0];
        for (int i = 1; i < array.Length; i++)
        {
            array[i] = vertices[^i];
        }

        return array;
    }

    public static WindingOrder DetermineWindingOrder(Vector2[] vertices)
    {
        int num = 0;
        int num2 = 0;
        Vector2 val = vertices[0];
        for (int i = 1; i < vertices.Length; i++)
        {
            Vector2 val2 = vertices[i];
            Vector2 val3 = vertices[(i + 1) % vertices.Length];
            Vector2 val4 = val - val2;
            Vector2 val5 = val3 - val2;
            if (val4.X * val5.Y - val4.Y * val5.X >= 0f)
            {
                num++;
            }
            else
            {
                num2++;
            }

            val = val2;
        }

        if (num <= num2)
        {
            return WindingOrder.CounterClockwise;
        }

        return WindingOrder.Clockwise;
    }

    private void ClipNextEar(ICollection<Triangle> triangles)
    {
        Vertex value = earVertices[0].Value;
        Vertex value2 = polygonVertices[polygonVertices.IndexOf(value) - 1].Value;
        Vertex value3 = polygonVertices[polygonVertices.IndexOf(value) + 1].Value;
        triangles.Add(new Triangle(value, value3, value2));
        earVertices.RemoveAt(0);
        polygonVertices.RemoveAt(polygonVertices.IndexOf(value));
        ValidateAdjacentVertex(value2);
        ValidateAdjacentVertex(value3);
    }

    private void ValidateAdjacentVertex(Vertex vertex)
    {
        if (reflexVertices.Contains(vertex) && IsConvex(vertex))
        {
            reflexVertices.Remove(vertex);
            convexVertices.Add(vertex);
        }

        if (convexVertices.Contains(vertex))
        {
            bool flag = earVertices.Contains(vertex);
            bool flag2 = IsEar(vertex);
            if (flag && !flag2)
            {
                earVertices.Remove(vertex);
            }
            else if (!flag && flag2)
            {
                earVertices.AddFirst(vertex);
            }
        }
    }

    private void FindConvexAndReflexVertices()
    {
        for (int i = 0; i < polygonVertices.Count; i++)
        {
            Vertex value = polygonVertices[i].Value;
            if (IsConvex(value))
            {
                convexVertices.Add(value);
            }
            else
            {
                reflexVertices.Add(value);
            }
        }
    }

    private void FindEarVertices()
    {
        for (int i = 0; i < convexVertices.Count; i++)
        {
            Vertex vertex = convexVertices[i];
            if (IsEar(vertex))
            {
                earVertices.AddLast(vertex);
            }
        }
    }

    private bool IsEar(Vertex c)
    {
        Vertex value = polygonVertices[polygonVertices.IndexOf(c) - 1].Value;
        Vertex value2 = polygonVertices[polygonVertices.IndexOf(c) + 1].Value;
        foreach (Vertex reflexVertex in reflexVertices)
        {
            if (!reflexVertex.Equals(value) && !reflexVertex.Equals(c) && !reflexVertex.Equals(value2) &&
                Triangle.ContainsPoint(value, c, value2, reflexVertex))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsConvex(Vertex c)
    {
        Vertex value = polygonVertices[polygonVertices.IndexOf(c) - 1].Value;
        Vertex value2 = polygonVertices[polygonVertices.IndexOf(c) + 1].Value;
        Vector2 val = Normalize(c.Position - value.Position);
        Vector2 val2 = Normalize(value2.Position - c.Position);
        Vector2 val3 = default(Vector2);
        val3 = new Vector2(0f - val2.Y, val2.X);
        return val.X * val3.X + val.Y * val3.Y <= 0f;
    }

    private static Vector2 Normalize(Vector2 v)
    {
        float inv = 1f / (float)Math.Sqrt(v.X * v.X + v.Y * v.Y);
        return new Vector2(v.X * inv, v.Y * inv);
    }

    private bool IsReflex(Vertex c)
    {
        return !IsConvex(c);
    }
}
