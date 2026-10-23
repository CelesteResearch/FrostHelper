// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Geometry;
using Celeste.Core.Mathematics;
using Celeste.Core.Randomness;
using Celeste.Core.Rendering;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Display;
using Celeste.Graphics.Rendering;
using Celeste.Graphics.Geometry.TwoD.Camera;
using Celeste.Graphics.Rendering.Vertices;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Environment.Lava;
using Celeste.Scene.Entities;
using Celeste.Mods.FrostHelper.Modules;
using Celeste.Mods.FrostHelper.Runtime.Lava;

namespace Celeste.Mods.FrostHelper.Components.Lava
{
    internal sealed class CustomLavaRect : SourceComponentState
    {
        internal struct WaveData
        {
            public float Amplitude;

            public float WaveNumber;

            public float Frequency;

            public float Phase;

            public WaveData(float amplitude, float waveNumber, float frequency)
            {
                Amplitude = amplitude;
                WaveNumber = waveNumber;
                Frequency = frequency;
                Phase = 0f;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float Get(float val, float timer)
            {
                return Sin(val * WaveNumber + timer * Frequency + Phase) * Amplitude;
            }

            public static List<WaveData> ParseWaves(string waves)
            {
                var list = new List<WaveData>();
                foreach (string segment in waves.Split(';'))
                {
                    var fields = segment.Split(',');
                    WaveData wave = default;
                    if (Read(fields, 0, out wave.Amplitude) && Read(fields, 1, out wave.WaveNumber) &&
                        Read(fields, 2, out wave.Frequency))
                        Read(fields, 3, out wave.Phase);
                    if (wave.Amplitude != 0f) list.Add(wave);
                }

                return list;
            }

            private static bool Read(string[] fields, int index, out float value)
            {
                value = 0f;
                return index < fields.Length &&
                       float.TryParse(fields[index], NumberStyles.Float, CultureInfo.InvariantCulture, out value);
            }
        }

        [Flags]
        internal enum RainbowModes
        {
            None = 0,
            Surface = 1,
            Edge = 2,
            Bubble = 4,
            All = 7
        }

        private interface IWaveProvider
        {
            float Wave(CustomLavaRect rect, int step, float length);
        }

        [StructLayout(LayoutKind.Sequential, Size = 1)]
        private readonly struct DefaultWaveProvider : IWaveProvider
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float Wave(CustomLavaRect rect, int step, float length)
            {
                return rect.Wave(step, length);
            }
        }

        private readonly unsafe struct PrecalculatedWaveProvider : IWaveProvider
        {
            private readonly float* waves;
            public PrecalculatedWaveProvider(float* waves) => this.waves = waves;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public unsafe float Wave(CustomLavaRect rect, int step, float length)
            {
                return waves[step];
            }
        }

        public enum OnlyModes
        {
            All,
            OnlyTop,
            OnlyBottom
        }

        public struct Bubble
        {
            public Vector2 Position;

            public float Speed;

            public float Alpha;

            public byte Type;
        }

        public struct SurfaceBubble
        {
            public float X;

            public float Frame;

            public byte Animation;
        }

        internal List<WaveData> Waves;

        public Vector2 Position = Vector2.Zero;

        public float Fade = 16f;

        public float Spikey;

        public OnlyModes OnlyMode;

        public float CurveAmplitude = 12f;

        public float UpdateMultiplier = 1f;

        public PackedColor SurfaceColor = PackedColor.White;

        public PackedColor EdgeColor = PackedColor.LightGray;

        public PackedColor CenterColor = PackedColor.DarkGray;

        internal RainbowModes IsRainbow;

        private float _timer;
        private readonly IOriginalLavaProcess engine;
        private readonly FrostHelperModule module;

        private SourceVertexPositionColor[] _verts;

        private bool _dirty;

        private int _vertCount;

        private Bubble[] _bubbles;

        private SurfaceBubble[] _surfaceBubbles;

        private int _surfaceBubbleIndex;

        private List<List<AtlasEntry>> _surfaceBubbleAnimations;

        private float _bubbleAmountMultiplier;

        public bool HasBubbles => _bubbleAmountMultiplier > 0f;

        public int SurfaceStep { get; set; }

        public float Width { get; set; }

        public float Height { get; set; }

        private List<AtlasEntry> _bubbleTextures { get; set; }

        private void SetSurfaceBubbleAnimations(ReadOnlySpan<char> key)
        {
            _surfaceBubbleAnimations = new List<List<AtlasEntry>>();
            foreach (string entry in key.ToString().Split(';'))
                _surfaceBubbleAnimations.Add(engine.LiquidAtlas.GetAtlasSubtextures(entry));
        }

        private void SetBubbleTextures(ReadOnlySpan<char> key)
        {
            _bubbleTextures = new List<AtlasEntry>();
            foreach (string entry in key.ToString().Split(';')) _bubbleTextures.Add(engine.LiquidAtlas[entry]);
        }

        private void SetupBubbles(string config)
        {
            int separator = config.IndexOf('|');
            var value = separator < 0 ? config : config[..separator];
            float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _bubbleAmountMultiplier);
            string textures = "particles/bubble", animations = "danger/lava/bubble_a";
            if (separator >= 0 && separator + 1 < config.Length)
            {
                string remaining = config[(separator + 1)..];
                int next = remaining.IndexOf('|');
                string entry = next < 0 ? remaining : remaining[..next];
                if (entry.Length != 0) textures = entry;
                if (next >= 0 && next + 1 < remaining.Length) animations = remaining[(next + 1)..];
            }

            SetBubbleTextures(textures.AsSpan());
            SetSurfaceBubbleAnimations(animations.AsSpan());
        }

        public CustomLavaRect(float width, float height, int step, string bubbleConfig, IOriginalLavaProcess engine,
            FrostHelperModule module)
            : base(true, true)
        {
            this.engine = engine;
            this.module = module;
            _timer = SourceRandomMath.NextFloat(engine.Random, 100f);
            SetupBubbles(bubbleConfig);
            Resize(width, height, step);
        }

        public void Resize(float width, float height, int step)
        {
            Width = width;
            Height = height;
            SurfaceStep = step;
            _dirty = true;
            _verts = new SourceVertexPositionColor[(int)((double)width / (double)SurfaceStep * 2.0 +
                                                         (double)height / (double)SurfaceStep * 2.0 + 4.0) * 3 * 6 + 6];
            if (HasBubbles)
            {
                _bubbles = new Bubble[(int)((double)width * (double)height * 0.004999999888241291 *
                                            (double)_bubbleAmountMultiplier)];
                _surfaceBubbles = new SurfaceBubble[(int)Math.Max(4.0, (double)_bubbles.Length * 0.25)];
                for (int i = 0; i < _bubbles.Length; i++)
                {
                    ref Bubble reference = ref _bubbles[i];
                    reference.Position = new Vector2(1f + SourceRandomMath.NextFloat(engine.Random, Width - 2f),
                        SourceRandomMath.NextFloat(engine.Random, Height));
                    reference.Speed = SourceRandomMath.Range(engine.Random, 4, 12);
                    reference.Alpha = SourceRandomMath.Range(engine.Random, 0.4f, 0.8f);
                    reference.Type = (byte)((_bubbleTextures.Count != 1)
                        ? ((byte)engine.Random.Next(_bubbleTextures.Count))
                        : 0);
                }

                for (int j = 0; j < _surfaceBubbles.Length; j++)
                {
                    _surfaceBubbles[j].X = -1f;
                }
            }
        }

        private IntRect GetCullRect()
        {
            Vector2 val = EntityAs<SceneEntityState>().Position + Position;
            IntRect result = default(IntRect);
            result = new IntRect((int)val.X, (int)val.Y, (int)Width, (int)Height);
            result.Inflate(16, 16);
            return result;
        }

        private bool IsVisible(out IntRect cullRect)
        {
            object scene = Scene;
            CameraState camera = ((OriginalLevelSceneRoot)scene).Camera;
            cullRect = GetCullRect();
            return LavaCameraCull.IsRectangleVisible(cullRect, 4f, camera);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void Update()
        {
            if (!IsVisible(out var _))
            {
                return;
            }

            _timer += UpdateMultiplier * engine.DeltaTime;
            if ((double)UpdateMultiplier != 0.0 || IsRainbow != RainbowModes.None)
            {
                _dirty = true;
            }

            if (HasBubbles)
            {
                Bubble[] bubbles = _bubbles;
                for (int i = 0; i < bubbles.Length; i++)
                {
                    ref Bubble reference = ref bubbles[i];
                    reference.Position.Y -= UpdateMultiplier * reference.Speed * engine.DeltaTime;
                    if ((double)reference.Position.Y <
                        2.0 - (double)Wave((int)((double)reference.Position.X / (double)SurfaceStep), Width))
                    {
                        reference.Position.Y = Height - 1f;
                        if ((SourceRandomMath.NextFloat(engine.Random) < 0.75f))
                        {
                            ref SurfaceBubble reference2 = ref _surfaceBubbles[_surfaceBubbleIndex];
                            reference2.X = reference.Position.X;
                            reference2.Frame = 0f;
                            reference2.Animation = (byte)engine.Random.Next(_surfaceBubbleAnimations.Count);
                            _surfaceBubbleIndex = (_surfaceBubbleIndex + 1) % _surfaceBubbles.Length;
                        }
                    }
                }

                SurfaceBubble[] surfaceBubbles = _surfaceBubbles;
                for (int j = 0; j < surfaceBubbles.Length; j++)
                {
                    ref SurfaceBubble reference3 = ref surfaceBubbles[j];
                    if ((double)reference3.X >= 0.0)
                    {
                        reference3.Frame += engine.DeltaTime * 6f;
                        if (reference3.Frame >= (float)_surfaceBubbleAnimations[reference3.Animation].Count)
                        {
                            reference3.X = -1f;
                        }
                    }
                }
            }

            base.Update();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Sin(float value)
        {
            return (1f + MathF.Sin(value)) / 2f;
        }

        public float Wave(int step, float length)
        {
            int num = step * SurfaceStep;
            float num2 = ((OnlyMode != OnlyModes.All)
                ? 1f
                : (ScalarMath.ClampedMap((float)num, 0f, length * 0.1f, 0f, 1f) *
                   ScalarMath.ClampedMap((float)num, length * 0.9f, length, 1f, 0f)));
            float num3 = 0f;
            List<WaveData> span = Waves;
            for (int i = 0; i < span.Count; i++)
            {
                WaveData waveData = span[i];
                num3 += waveData.Get(num, _timer);
            }

            if (step % 2 == 0)
            {
                num3 += Spikey;
            }

            if (OnlyMode != OnlyModes.All)
            {
                num3 += (1f - ScalarMath.YoYo((float)num / length)) * CurveAmplitude;
            }

            return num3 * num2;
        }

        public void Quad(ref int vert, Vector2 va, Vector2 vb, Vector2 vc, Vector2 vd, PackedColor color)
        {
            Quad(ref vert, va, color, vb, color, vc, color, vd, color);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Quad(ref int vert, Vector2 va, PackedColor ca, Vector2 vb, PackedColor cb, Vector2 vc,
            PackedColor cc,
            Vector2 vd, PackedColor cd)
        {
            VertexColorQuadWriter.Write(_verts, ref vert, va, ca, vb, cb, vc, cc, vd, cd);
        }

        private void Edge<TWave>(ref int vert, Vector2 a, Vector2 b, float fade, float insetFade, TWave waveProvider,
            IntRect cullRect) where TWave : struct, IWaveProvider
        {
            float num = (a - b).Length();
            float num2 = num / (float)SurfaceStep;
            Vector2 vector = new Vector2(ScalarMath.Clamp(a.X, cullRect.Left, cullRect.Right),
                ScalarMath.Clamp(a.Y, cullRect.Top, cullRect.Bottom));
            Vector2 vector2 = new Vector2(ScalarMath.Clamp(b.X, cullRect.Left, cullRect.Right),
                ScalarMath.Clamp(b.Y, cullRect.Top, cullRect.Bottom));
            float num3 = Math.Max(fade, insetFade) / (float)SurfaceStep / 2f;
            float num4 = (vector - vector2).Length() / (float)SurfaceStep;
            int x = (int)Math.Ceiling((a - vector).Length() / (float)SurfaceStep - num3);
            x = Math.Max(x, 0);
            num4 += (float)x;
            num4 = Math.Min(num2, num4 + num3 * 2f);
            if ((float)x > num4)
            {
                return;
            }

            float num5 = ((OnlyMode == OnlyModes.All) ? (insetFade / num) : 0f);
            Vector2 vector3 = Perpendicular(SafeNormalize(b - a));
            float num6 = waveProvider.Wave(this, x, num);
            Vector2 vector4 = a - vector3 * num6;
            PackedColor surfacePackedColor = GetSurfaceColor(vector4);
            PackedColor cd = GetSurfaceColor(vector4 + vector3);
            PackedColor edgePackedColor = GetEdgeColor(vector4 + vector3);
            Vector2 vector5 = Lerp(a, b, num5);
            PackedColor val = GetCenterColor(vector5 + vector3 * (fade - num6));
            for (int i = x + 1; (float)i <= num4; i++)
            {
                float num7 = (float)i / num2;
                if (num7 > 1f)
                {
                    num7 = 1f;
                }

                Vector2 vector6 = Lerp(a, b, num7);
                float num8 = waveProvider.Wave(this, i, num);
                Vector2 vector7 = vector6 - vector3 * num8;
                Vector2 vector8 = Lerp(a, b, ScalarMath.ClampedMap(num7, 0f, 1f, num5, 1f - num5));
                Vector2 vector9 = vector8 + vector3 * (fade - num8);
                PackedColor centerPackedColor = GetCenterColor(vector9);
                Vector2 vector10 = vector5 + vector3 * (fade - num6);
                Quad(ref vert, vector4 + vector3, edgePackedColor, vector7 + vector3,
                    edgePackedColor = GetEdgeColor(vector7 + vector3), vector9, centerPackedColor, vector10, val);
                Quad(ref vert, vector10, val, vector9, centerPackedColor, vector8 + vector3 * fade,
                    GetCenterColor(vector5 + vector3 * fade), vector5 + vector3 * fade,
                    GetCenterColor(vector5 + vector3 * fade));
                PackedColor surfacePackedColor2 = GetSurfaceColor(vector7 + vector3);
                Quad(ref vert, vector4, surfacePackedColor, vector7, surfacePackedColor = GetSurfaceColor(vector7),
                    vector7 + vector3, surfacePackedColor2, vector4 + vector3, cd);
                num6 = num8;
                vector4 = vector7;
                cd = surfacePackedColor2;
                vector5 = vector8;
                val = centerPackedColor;
            }
        }

        public unsafe override void Render(ISourceRenderContext context)
        {
            if (!IsVisible(out var cullRect))
            {
                return;
            }

            engine.GameplayBatchScope.End(context);
            object scene = Scene;
            OriginalLevelSceneRoot val = (OriginalLevelSceneRoot)scene;
            CameraState camera = val.Camera;
            Vector2 val2 = EntityAs<SceneEntityState>().Position + Position;
            IntRect r = ((UpdateMultiplier == 0f || val.Transitioning)
                ? cullRect
                : LavaCameraCull.GetVisibleSection(cullRect, 4, camera));
            if (_dirty || (val.Transitioning && IsRainbow != RainbowModes.None))
            {
                Vector2 vector = default(Vector2);
                Vector2 vector2 = new Vector2(Width, 0f);
                Vector2 vector3 = new Vector2(0f, Height);
                Vector2 vector4 = new Vector2(Width, Height);
                Vector2 vector5 = new Vector2(Math.Min(Fade, Width / 2f), Math.Min(Fade, Height / 2f));
                IntRect cullRect2 = MovedBy(r, -val2);
                _vertCount = 0;
                if (OnlyMode == OnlyModes.All)
                {
                    int num = (int)Math.Ceiling(Width / (float)SurfaceStep) + 1;
                    Span<float> span = stackalloc float[num];
                    for (int i = 0; i < num; i++)
                    {
                        span[i] = Wave(i, Width);
                    }

                    int num2 = (int)Math.Ceiling(Height / (float)SurfaceStep) + 1;
                    Span<float> span2 = ((Width != Height) ? stackalloc float[num2] : span);
                    Span<float> span3 = span2;
                    if (Width != Height)
                    {
                        for (int j = 0; j < num2; j++)
                        {
                            span3[j] = Wave(j, Height);
                        }
                    }

                    PrecalculatedWaveProvider waveProvider =
                        new PrecalculatedWaveProvider((float*)Unsafe.AsPointer(ref span[0]));
                    PrecalculatedWaveProvider waveProvider2 =
                        new PrecalculatedWaveProvider((float*)Unsafe.AsPointer(ref span3[0]));
                    Edge(ref _vertCount, vector, vector2, vector5.Y, vector5.X, waveProvider, cullRect2);
                    Edge(ref _vertCount, vector2, vector4, vector5.X, vector5.Y, waveProvider2, cullRect2);
                    Edge(ref _vertCount, vector4, vector3, vector5.Y, vector5.X, waveProvider, cullRect2);
                    Edge(ref _vertCount, vector3, vector, vector5.X, vector5.Y, waveProvider2, cullRect2);
                    Quad(ref _vertCount, vector + vector5, GetCenterColor(vector + vector5),
                        vector2 + new Vector2(0f - vector5.X, vector5.Y),
                        GetCenterColor(vector2 + new Vector2(0f - vector5.X, vector5.Y)), vector4 - vector5,
                        GetCenterColor(vector4 - vector5), vector3 + new Vector2(vector5.X, 0f - vector5.Y),
                        GetCenterColor(vector3 + new Vector2(vector5.X, 0f - vector5.Y)));
                }
                else if (OnlyMode == OnlyModes.OnlyTop)
                {
                    Edge(ref _vertCount, vector, vector2, vector5.Y, 0f, default(DefaultWaveProvider), cullRect2);
                    Quad(ref _vertCount, vector + new Vector2(0f, vector5.Y), vector2 + new Vector2(0f, vector5.Y),
                        vector4,
                        vector3, CenterColor);
                }
                else if (OnlyMode == OnlyModes.OnlyBottom)
                {
                    Edge(ref _vertCount, vector4, vector3, vector5.Y, 0f, default(DefaultWaveProvider), cullRect2);
                    Quad(ref _vertCount, vector, vector2, vector4 + new Vector2(0f, 0f - vector5.Y),
                        vector3 + new Vector2(0f, 0f - vector5.Y), CenterColor);
                }

                _dirty = false;
            }

            module.LavaBuffers.Draw(context, engine, val, val2, _verts, _vertCount);
            if (!HasBubbles)
            {
                return;
            }

            Bubble[] bubbles = _bubbles;
            List<AtlasEntry> span4 = _bubbleTextures;
            for (int k = 0; k < bubbles.Length; k++)
            {
                ref Bubble reference = ref bubbles[k];
                Vector2 val4 = val2 + reference.Position;
                if (r.Contains((int)val4.X, (int)val4.Y))
                {
                    SourceAtlasDrawing.DrawCentered(context, Entity, span4[reference.Type], val4,
                        GetBubbleColor(val4) * bubbles[k].Alpha);
                }
            }

            SurfaceBubble[] surfaceBubbles = _surfaceBubbles;
            for (int l = 0; l < surfaceBubbles.Length; l++)
            {
                ref SurfaceBubble reference2 = ref surfaceBubbles[l];
                if ((double)reference2.X >= 0.0)
                {
                    AtlasEntry val5 = _surfaceBubbleAnimations[reference2.Animation][(int)reference2.Frame];
                    int num3 = (int)(reference2.X / (float)SurfaceStep);
                    float num4 = 1f - Wave(num3, Width);
                    Vector2 val6 = val2 + new Vector2((float)(num3 * SurfaceStep), num4);
                    if (r.Contains((int)val6.X, (int)val6.Y))
                    {
                        SourceAtlasDrawing.DrawJustified(context, Entity, val5, val6, new Vector2(0.5f, 1f),
                            GetBubbleColor(val6));
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PackedColor GetBubbleColor(Vector2 pos)
        {
            if ((IsRainbow & RainbowModes.Bubble) == 0)
            {
                return SurfaceColor;
            }

            return module.Spinners.GetHue(Scene, EntityAs<SceneEntityState>().Position + pos);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PackedColor GetSurfaceColor(Vector2 pos)
        {
            if ((IsRainbow & RainbowModes.Surface) == 0)
            {
                return SurfaceColor;
            }

            return module.Spinners.GetHue(Scene, EntityAs<SceneEntityState>().Position + pos);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PackedColor GetEdgeColor(Vector2 pos)
        {
            if ((IsRainbow & RainbowModes.Edge) == 0)
            {
                return EdgeColor;
            }

            return MultiplyNoAlpha(module.Spinners.GetHue(Scene, EntityAs<SceneEntityState>().Position + pos), 0.8f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public PackedColor GetCenterColor(Vector2 pos)
        {
            return CenterColor;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static PackedColor MultiplyNoAlpha(PackedColor value, float scale)
        {
            return new PackedColor((int)((float)(int)value.R * scale), (int)((float)(int)value.G * scale),
                (int)((float)(int)value.B * scale), (int)value.A);
        }

        private Vector2 Lerp(Vector2 a, Vector2 b, float amount) => Vector2.Lerp(a, b, amount);
        private static Vector2 SafeNormalize(Vector2 v) => v == Vector2.Zero ? Vector2.Zero : Vector2.Normalize(v);
        private static Vector2 Perpendicular(Vector2 value) => new(-value.Y, value.X);

        private static IntRect MovedBy(IntRect value, Vector2 offset) =>
            new(value.X + (int)offset.X, value.Y + (int)offset.Y, value.Width, value.Height);
    }
}
