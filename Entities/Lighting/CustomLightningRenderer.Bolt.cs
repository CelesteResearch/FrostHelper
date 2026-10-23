using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Randomness;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Core.Settings.Lightning;
using Celeste.Graphics.Rendering;

namespace Celeste.Mods.FrostHelper.Entities.Lighting;

public partial class CustomLightningRenderer
{
    [RuntimeTypeAlias("FrostHelper.CustomLightningRenderer+Bolt")]
    public class Bolt
    {
        private readonly CustomLightningRenderer owner;
        public List<Vector2> Nodes = new();
        public SimulationCoroutine Routine;
        public bool Visible;
        public float Size, Gap, Alpha, Flash, Scale;
        public uint Seed;
        public PackedColor Color;
        public int Width, Height;

        public Bolt(CustomLightningRenderer owner, PackedColor color, float scale, int width, int height)
        {
            this.owner = owner;
            Color = color;
            Width = width;
            Height = height;
            Scale = scale;
            Routine = new SimulationCoroutine(owner.engine, Run());
        }

        private float Range(float min, float max) => SourceRandomMath.Range(owner.random, min, max);
        private int Range(int min, int max) => SourceRandomMath.Range(owner.random, min, max);
        private bool AllowFlash => ((ISourceLightningFlashEnvironment)owner.engine).LightningFlashPolicy.AllowLightning;

        public void Update(object scene)
        {
            Routine.Update();
            Flash = ScalarMath.Approach(Flash, 0f, owner.engine.DeltaTime * 2f,
                ((ISourceExtremaEnvironment)owner.engine).ExtremaArithmetic);
        }

        private IEnumerator Run()
        {
            yield return Range(0f, 4f);
            var border = new Vector2[6];
            var inner = new Vector2[3];
            while (true)
            {
                for (int i = 0; i < 3; i++)
                {
                    var left = new Vector2(0f, Range(8, Height - 16));
                    var top = new Vector2(Range(8, Width - 16), 0f);
                    var right = new Vector2(Width, Range(8, Height - 16));
                    var bottom = new Vector2(Range(8, Width - 16), Height);
                    int choice = owner.random.Next(4);
                    var start = choice == 0 ? left : choice == 1 ? top : choice == 2 ? right : bottom;
                    var end = start.X <= 0f || start.X >= Width
                        ? new Vector2(Width - start.X, start.Y)
                        : new Vector2(start.X, Height - start.Y);
                    border[i * 2] = start;
                    border[i * 2 + 1] = end;
                }

                for (int i = 0; i < 3; i++)
                    inner[i] = new Vector2(Range(.25f, .75f) * Width, Range(.25f, .75f) * Height);
                Nodes.Clear();
                foreach (var point in border)
                {
                    Nodes.Add(point);
                    var nearest = inner[0];
                    float distance = owner.arithmetic.DistanceSquared(nearest, point);
                    for (int i = 1; i < inner.Length; i++)
                    {
                        float candidate = owner.arithmetic.DistanceSquared(inner[i], point);
                        if (candidate < distance)
                        {
                            distance = candidate;
                            nearest = inner[i];
                        }
                    }

                    Nodes.Add(nearest);
                }

                var previous = inner[inner.Length - 1];
                foreach (var point in inner)
                {
                    Nodes.Add(previous);
                    Nodes.Add(point);
                    previous = point;
                }

                Flash = 1f;
                Visible = true;
                Size = 5f;
                Gap = 0f;
                Alpha = 1f;
                for (int i = 0; i < 4; i++)
                {
                    Seed = (uint)owner.random.Next();
                    yield return .1f;
                }

                for (int i = 0; i < 5; i++)
                {
                    if (AllowFlash) Visible = false;
                    yield return .05f + i * .02f;
                    float percent = (float)i / 5f;
                    Visible = true;
                    Size = (1f - percent) * 5f;
                    Gap = percent;
                    Alpha = 1f - percent;
                    Visible = true;
                    Seed = (uint)owner.random.Next();
                    yield return .025f;
                }

                Visible = false;
                yield return Range(4f, 8f);
            }
        }

        public void Render(ISourceRenderContext context)
        {
            if (Flash > 0f && AllowFlash)
                SourcePrimitiveDrawing.Rect(context, owner, 0f, 0f, Width, Height,
                    PackedColor.White * Flash * .15f * Scale);
            if (Visible)
                for (int i = 0; i < Nodes.Count; i += 2)
                    DrawFatLightning(context, owner, Seed, Nodes[i], Nodes[i + 1], Size * Scale, Gap, Color * Alpha,
                        owner.arithmetic);
        }
    }
}
