using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Components.Rendering;
using Celeste.Core.Display;
using Celeste.Core.Geometry;
using Celeste.Core.Mathematics;
using Celeste.Core.Randomness;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Runtime;
using Celeste.Graphics.Geometry.TwoD.Camera;
using Celeste.Graphics.Rendering;
using Celeste.Graphics.Rendering.Primitives;
using Celeste.Graphics.Rendering.Vertices;
using Celeste.Mods.FrostHelper.Runtime.Lighting;
using Celeste.Mods.FrostHelper.Runtime.Rendering;
using Celeste.Mods.FrostHelper.Sessions.Lighting;
using Celeste.Scene.Entities;
using Celeste.Scene.Scheduling.ExecutionTags;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;

namespace Celeste.Mods.FrostHelper.Entities.Lighting;

[RuntimeTypeAlias("FrostHelper.CustomLightningRenderer")]
[SceneTracked(false)]
public partial class CustomLightningRenderer : SceneEntityState, IFrostLightningGroupRenderer
{
    private readonly ISourceEngineContext engine;
    private readonly ISourceRenderEnvironment environment;
    private readonly FrostLightningColors colors;
    private readonly ISourceRandomEnvironment randomness;
    private ISourceRandom random => randomness.Random;
    private readonly ISourceVectorArithmetic arithmetic;
    private readonly ISourceCurveArithmetic curveArithmetic;
    public List<Edge> edges = new();
    public List<Bolt> Bolts = new();
    public List<Fill> fills = new();
    private readonly List<ArbitraryFill> arbitraryFills = new();
    public SourceVertexPositionColor[] edgeVerts = new SourceVertexPositionColor[1024];
    private int? cachedBloomVertsCount;
    private SourceVertexPositionColor[] bloomVerts = new SourceVertexPositionColor[1024];
    public PackedColor[] ElectricityColors;
    public PackedColor[] electricityColorsLerped;
    private float fade;
    public uint edgeSeed, leapSeed;
    private readonly uint[] edgeSeeds;
    public bool UpdateSeeds = true, DrawEdges = true;
    public Config Cfg;

    public float Fade
    {
        get => fade;
        set
        {
            fade = value;
            MarkDirty();
        }
    }

    public bool AffectedByLightningBoxes => Cfg.AffectedByBreakerBoxes;
    public PackedColor FillColor => Cfg.FillColor;
    public string GroupId => Cfg.GroupId;

    private LightningGroupChange SessionChange =>
        colors.Session.ArbitraryLightningGroupChanges.TryGetValue(GroupId, out var value) ? value : null;

    public CustomLightningRenderer(ISourceEngineContext engine, Config cfg) : base(
        ((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration)
    {
        this.engine = engine;
        Cfg = cfg;
        environment = (ISourceRenderEnvironment)engine;
        randomness = (ISourceRandomEnvironment)engine;
        arithmetic = ((ISourceVectorEnvironment)engine).VectorArithmetic;
        curveArithmetic = ((ISourceCurveEnvironment)engine).CurveArithmetic;
        colors = ((ISourceEverestModulesEnvironment)engine).Modules.OfType<IFrostLightningEnvironment>().Single()
            .Lightning;
        Tags = ((ISourceGlobalFrozenExecutionTagsEnvironment)engine).GlobalFrozenExecutionTags.Global.Value |
               ((ISourcePersistentTransitionExecutionTagsEnvironment)engine).PersistentTransitionExecutionTags
               .TransitionUpdate.Value;
        Depth = cfg.Depth;
        ElectricityColors = new PackedColor[cfg.Bolts.Length];
        for (int i = 0; i < cfg.Bolts.Length; i++) ElectricityColors[i] = cfg.Bolts[i].Color;
        electricityColorsLerped = new PackedColor[ElectricityColors.Length];
        edgeSeeds = new uint[cfg.Bolts.Length];
        Add(new CustomBloomCallbackComponentState(OnRenderBloom));
    }

    public CustomLightningRenderer(ISourceEngineContext engine) : this(engine, Config.Default)
    {
    }

    public static CustomLightningRenderer GetOrCreate(ISourceEngineContext engine, object scene, Config config)
    {
        var scheduler = ((ISourceScene)scene).SourceScene;
        if (scheduler.Entities.TryGetTrackedEntities(typeof(CustomLightningRenderer), out var tracked))
            foreach (CustomLightningRenderer renderer in tracked)
                if (renderer.Cfg == config)
                    return renderer;
        foreach (var queued in scheduler.GetToAdd())
            if (queued is CustomLightningRenderer renderer && renderer.Cfg == config)
                return renderer;
        var created = new CustomLightningRenderer(engine, config);
        scheduler.QueueSourceEntity(created);
        return created;
    }

    public void Apply(LightningGroupChange change)
    {
        if (change.Depth is { } depth) Depth = depth;
        SetColors(change.Colors);
    }

    public void SetColors(PackedColor[] palette)
    {
        int count = Math.Min(ElectricityColors.Length, palette.Length);
        for (int i = 0; i < count; i++) ElectricityColors[i] = palette[i];
        for (int i = 0; i < Bolts.Count; i++) Bolts[i].Color = palette[i % palette.Length];
    }

    public override void Awake(object scene)
    {
        base.Awake(scene);
        if (GroupId == "" && colors.Session.LightningColorA != null)
        {
            colors.GetColors(out var a, out var b);
            SetColors(new[] { a, b });
        }

        if (colors.Session.ArbitraryLightningGroupChanges.TryGetValue(GroupId, out var value)) SetColors(value.Colors);
    }

    public override void SceneEnd(object scene)
    {
        base.SceneEnd(scene);
        edges = new();
        Bolts = new();
    }

    public void MarkDirty() => cachedBloomVertsCount = null;

    public void Add(Edge edge)
    {
        edges.Add(edge);
        MarkDirty();
    }

    public void Add(Bolt bolt)
    {
        Bolts.Add(bolt);
        MarkDirty();
    }

    public void Add(Fill fill)
    {
        fills.Add(fill);
        MarkDirty();
    }

    public void Add(ArbitraryFill fill)
    {
        arbitraryFills.Add(fill);
        MarkDirty();
    }

    public void Remove(Edge edge)
    {
        edges.Remove(edge);
        MarkDirty();
    }

    public void Remove(Bolt bolt)
    {
        Bolts.Remove(bolt);
        MarkDirty();
    }

    public void Remove(Fill fill)
    {
        fills.Remove(fill);
        MarkDirty();
    }

    public void Remove(ArbitraryFill fill)
    {
        arbitraryFills.Remove(fill);
        MarkDirty();
    }

    public void Reset()
    {
        UpdateSeeds = true;
        Fade = 0f;
    }

    public void ToggleEdges(bool immediate = false)
    {
        var camera = ((ISourceCameraScene)Scene).Camera;
        var view = new IntRect((int)camera.Left - 4, (int)camera.Top - 4, (int)(camera.Right - camera.Left) + 8,
            (int)(camera.Bottom - camera.Top) + 8);
        var clock = ((ISourceScene)Scene).SourceScene.Clock;
        for (int i = 0; i < edges.Count; i++)
        {
            var edge = edges[i];
            if (edge.ForcedInvisible) edge.Visible = false;
            else if (immediate) edge.Visible = edge.InView(ref view);
            else if (!edge.Visible && clock.OnInterval(.05f, i * .01f, engine) && edge.InView(ref view))
                edge.Visible = true;
            else if (edge.Visible && clock.OnInterval(.25f, i * .01f, engine) && !edge.InView(ref view))
                edge.Visible = false;
        }
    }

    public override void Update()
    {
        ToggleEdges();
        foreach (var bolt in Bolts) bolt.Update(Scene);
        if (!UpdateSeeds) return;
        var clock = ((ISourceScene)Scene).SourceScene.Clock;
        if (clock.OnInterval(.1f, engine)) edgeSeed = (uint)random.Next();
        if (clock.OnInterval(.7f, engine)) leapSeed = (uint)random.Next();
        for (int i = 0; i < Cfg.Bolts.Length; i++)
            if (Cfg.Bolts[i].UpdateInterval == .1f) edgeSeeds[i] = edgeSeed;
            else if (clock.OnInterval(Cfg.Bolts[i].UpdateInterval, engine)) edgeSeeds[i] = (uint)random.Next();
    }

    private static void EnsureCapacity(ref SourceVertexPositionColor[] verts, int count)
    {
        if (verts.Length < count) Array.Resize(ref verts, count);
    }

    public void RenderFills(ref int index, ref SourceVertexPositionColor[] verts, PackedColor color)
    {
        EnsureCapacity(ref verts, index + fills.Count * 6);
        foreach (var fill in fills)
        {
            var r = fill.R;
            verts[index].Color = color;
            verts[index++].Position = new Vector3(r.X, r.Y, 0);
            verts[index].Color = color;
            verts[index++].Position = new Vector3(r.X + r.Width, r.Y, 0);
            verts[index].Color = color;
            verts[index++].Position = new Vector3(r.X + r.Width, r.Y + r.Height, 0);
            verts[index].Color = color;
            verts[index++].Position = new Vector3(r.X, r.Y, 0);
            verts[index].Color = color;
            verts[index++].Position = new Vector3(r.X, r.Y + r.Height, 0);
            verts[index].Color = color;
            verts[index++].Position = new Vector3(r.X + r.Width, r.Y + r.Height, 0);
        }

        var camera = ((ISourceCameraScene)Scene).Camera;
        foreach (var fill in arbitraryFills)
        {
            var r = fill.RenderBounds;
            if (!FrostCameraCull.IsRectangleVisible(r.X, r.Y, r.Width, r.Height, 4f, camera)) continue;
            EnsureCapacity(ref verts, index + fill.Verts.Length);
            var offset = fill.Parent.Position;
            foreach (var vertex in fill.Verts)
            {
                verts[index].Color = color;
                verts[index++].Position = new Vector3(vertex.X + offset.X, vertex.Y + offset.Y, vertex.Z);
            }
        }
    }

    private void Submit(ISourceRenderContext context, SourceVertexPositionColor[] verts, int count, Matrix4x4 matrix)
    {
        var scope = ((ISourceGameplayBatchScopeEnvironment)engine).GameplayBatchScope;
        scope.End(context);
        SourcePrimitiveGraphics.DrawVertices(((ISourcePrimitiveGraphicsEnvironment)engine).PrimitiveGraphics, matrix,
            verts, count);
        scope.Begin(context);
    }

    private void OnRenderBloom()
    {
        var context = environment.CurrentContext;
        var camera = ((ISourceCameraScene)Scene).Camera;
        var color = PackedColor.White * (.25f + Fade * .75f);
        foreach (var edge in edges)
            if (edge.Visible)
                SourcePrimitiveDrawing.Line(context, this, edge.Parent.Position + edge.A, edge.Parent.Position + edge.B,
                    color, 4f, arithmetic);
        if (cachedBloomVertsCount == null)
        {
            int index = 0;
            RenderFills(ref index, ref bloomVerts, color);
            cachedBloomVertsCount = index;
        }

        if (cachedBloomVertsCount.Value > 0) Submit(context, bloomVerts, cachedBloomVertsCount.Value, camera.Matrix);
        cachedBloomVertsCount = null;
        if (Fade > 0f)
            SourcePrimitiveDrawing.Rect(context, this, camera.Position.X, camera.Position.Y, 320f, 180f,
                PackedColor.White * Fade);
    }

    public override void Render(ISourceRenderContext context)
    {
        base.Render(context);
        var camera = ((ISourceCameraScene)Scene).Camera;
        int index = 0;
        var trigger = colors.FirstEntered(GroupId);
        var change = SessionChange;
        var fillString = trigger?.FillColor;
        var color = fillString != null ? colors.ColorReader.Read(fillString) : change?.FillColor ?? Cfg.FillColor;
        float multiplier = trigger?.FillColorMultiplier ?? change?.FillColorMultiplier ?? 1f;
        RenderFills(ref index, ref edgeVerts, color * multiplier);
        if (edges.Count <= 0 || electricityColorsLerped.Length == 0) return;
        for (int i = 0; i < electricityColorsLerped.Length; i++)
            electricityColorsLerped[i] = PackedColor.Lerp(ElectricityColors[i], PackedColor.White, Fade);
        uint seed = leapSeed;
        foreach (var edge in edges)
            if (edge.Visible)
            {
                for (int i = 0; i < Cfg.Bolts.Length; i++)
                    DrawSimpleLightning(ref index, ref edgeVerts, unchecked(edgeSeeds[i] + (uint)i + edge.SeedOffset),
                        edge.Parent.Position, edge.A, edge.B, electricityColorsLerped[i], arithmetic,
                        Cfg.Bolts[i].Thickness + Fade * 3f);
                if (PseudoRand(ref seed) % 30 == 0)
                    DrawBezierLightning(ref index, ref edgeVerts, unchecked(edgeSeed + edge.SeedOffset),
                        edge.Parent.Position, edge.A, edge.B, 24f, 10,
                        electricityColorsLerped[electricityColorsLerped.Length - 1], curveArithmetic, arithmetic);
            }

        if (index > 0) Submit(context, edgeVerts, index, camera.Matrix);
    }
}
