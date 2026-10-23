using System.Numerics;
using Celeste.Formats.Map.Entities.Structure;

namespace Celeste.Mods.FrostHelper.Geometry.Triangulation
{
    public static class ArbitraryShapeFill
    {
        public static Vector3[] FromNodes(EntityData data, Vector2 offset, string cacheKey = "__cachedFillV3")
        {
            var points = new Vector2[data.Nodes.Length + 1];
            points[0] = data.Position + offset;
            for (int i = 1; i < points.Length; i++) points[i] = data.Nodes[i - 1] + offset;
            WindingOrder? winding = data.Attr("windingOrder") switch
            {
                "Clockwise" => WindingOrder.Clockwise,
                "CounterClockwise" => WindingOrder.CounterClockwise,
                _ => null
            };
            new ShapeTriangulator().Triangulate(points, WindingOrder.Clockwise, winding, out var vertices,
                out var indices);
            var result = new Vector3[indices.Length];
            for (int i = 0; i < indices.Length; i++) result[i] = new Vector3(vertices[indices[i]], 0f);
            data.Values[cacheKey] = result;
            return result;
        }
    }
}
