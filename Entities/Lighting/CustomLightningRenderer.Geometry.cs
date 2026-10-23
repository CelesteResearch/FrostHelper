using System;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Framework;
using Celeste.Core.Rendering;
using Celeste.Graphics.Rendering;
using Celeste.Graphics.Rendering.Vertices;

namespace Celeste.Mods.FrostHelper.Entities.Lighting;

public partial class CustomLightningRenderer
{
    private static int Hash(Vector2 v) => SourceSingleHashMath.GetVector2HashCodeInstalled(v);

    public static uint PseudoRand(ref uint seed)
    {
        seed ^= seed << 13;
        seed ^= seed >> 17;
        return seed;
    }

    public static float PseudoRandRange(ref uint seed, float min, float max) =>
        min + (float)(PseudoRand(ref seed) & 0x3FF) / 1024f * (max - min);

    public static void DrawSimpleLightning(ref int index, ref SourceVertexPositionColor[] verts, uint seed, Vector2 pos,
        Vector2 a, Vector2 b, PackedColor color, ISourceVectorArithmetic arithmetic, float thickness = 1f)
    {
        a += pos;
        b += pos;
        float length = arithmetic.Length(b - a);
        var direction = arithmetic.Divide(b - a, length);
        var normal = new Vector2(-direction.Y, direction.X);
        a += normal;
        b += normal;
        var half = normal * thickness;
        var previous = a;
        int sign = PseudoRand(ref seed) % 2 != 0 ? 1 : -1;
        float phase = PseudoRandRange(ref seed, 0f, (float)Math.PI * 2f);
        float progress = 0f;
        float maximum = index + (arithmetic.Length(b - a) / 4f + 1f) * 6f;
        while (maximum >= verts.Length) Array.Resize(ref verts, verts.Length * 2);
        for (int i = index; i < maximum; i++) verts[i].Color = color;
        do
        {
            float offset = PseudoRandRange(ref seed, 0f, 4f);
            phase += 0.1f;
            progress += 4f + offset;
            var next = a + direction * progress;
            next = progress < length ? next + (sign * normal * offset - normal) : b;
            verts[index++].Position = new Vector3(previous - half, 0f);
            verts[index++].Position = new Vector3(next - half, 0f);
            verts[index++].Position = new Vector3(next + half, 0f);
            verts[index++].Position = new Vector3(previous - half, 0f);
            verts[index++].Position = new Vector3(next + half, 0f);
            verts[index++].Position = new Vector3(previous, 0f);
            previous = next;
            sign = -sign;
        } while (progress < length);
    }

    public static void DrawBezierLightning(ref int index, ref SourceVertexPositionColor[] verts, uint seed, Vector2 pos,
        Vector2 a, Vector2 b, float anchor, int steps, PackedColor color, ISourceCurveArithmetic curveArithmetic,
        ISourceVectorArithmetic arithmetic)
    {
        a += pos;
        b += pos;
        var direction = arithmetic.SafeNormalize(b - a);
        var normal = new Vector2(-direction.Y, direction.X);
        var curve = new SimpleCurve(a, b, (b + a) / 2f + normal * anchor);
        int maximum = index + (steps + 2) * 6;
        while (maximum >= verts.Length) Array.Resize(ref verts, verts.Length * 2);
        var previous = curve.GetPoint(0f, curveArithmetic);
        for (int i = 0; i <= steps; i++)
        {
            var next = curve.GetPoint((float)i / steps, curveArithmetic);
            if (i != steps) next += new Vector2(PseudoRandRange(ref seed, -2f, 2f), PseudoRandRange(ref seed, -2f, 2f));
            verts[index].Position = new Vector3(previous - normal, 0f);
            verts[index++].Color = color;
            verts[index].Position = new Vector3(next - normal, 0f);
            verts[index++].Color = color;
            verts[index].Position = new Vector3(next, 0f);
            verts[index++].Color = color;
            verts[index].Position = new Vector3(previous - normal, 0f);
            verts[index++].Color = color;
            verts[index].Position = new Vector3(next, 0f);
            verts[index++].Color = color;
            verts[index].Position = new Vector3(previous, 0f);
            verts[index++].Color = color;
            previous = next;
        }
    }

    public static void DrawFatLightning(ISourceRenderContext context, object entity, uint seed, Vector2 a, Vector2 b,
        float size, float gap, PackedColor color, ISourceVectorArithmetic arithmetic)
    {
        seed += unchecked((uint)(Hash(a) + Hash(b)));
        float length = arithmetic.Length(b - a);
        var direction = arithmetic.Divide(b - a, length);
        var normal = new Vector2(-direction.Y, direction.X);
        var previous = a;
        int sign = 1;
        PseudoRandRange(ref seed, 0f, (float)Math.PI * 2f);
        float progress = 0f;
        do
        {
            progress += PseudoRandRange(ref seed, 10f, 14f);
            var next = a + direction * progress;
            next = progress < length ? next + sign * normal * PseudoRandRange(ref seed, 0f, 6f) : b;
            var end = next;
            if (gap > 0f)
            {
                end = previous + (next - previous) * (1f - gap);
                SourcePrimitiveDrawing.Line(context, entity, previous, next + direction, color, size * 0.5f,
                    arithmetic);
            }

            SourcePrimitiveDrawing.Line(context, entity, previous, end + direction, color, size, arithmetic);
            previous = next;
            sign = -sign;
        } while (progress < length);
    }
}
