using System;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Geometry;
using Celeste.Core.Reflection.Runtime;
using Celeste.Scene.Entities;

namespace Celeste.Mods.FrostHelper.Entities.Lighting;

public partial class CustomLightningRenderer
{
    [RuntimeTypeAlias("FrostHelper.CustomLightningRenderer+Config")]
    public readonly struct Config : IEquatable<Config>
    {
        [RuntimeTypeAlias("FrostHelper.CustomLightningRenderer+Config+BoltConfig")]
        public readonly struct BoltConfig : IEquatable<BoltConfig>
        {
            public readonly PackedColor Color;
            public readonly float Thickness, UpdateInterval;

            public BoltConfig(PackedColor color, float thickness, float updateInterval)
            {
                Color = color;
                Thickness = thickness;
                UpdateInterval = updateInterval;
            }

            public bool Equals(BoltConfig other) => Color == other.Color && Thickness.Equals(other.Thickness) &&
                                                    UpdateInterval.Equals(other.UpdateInterval);

            public override bool Equals(object other) => other is BoltConfig bolt && Equals(bolt);
            public override int GetHashCode() => HashCode.Combine(Color, Thickness, UpdateInterval);
            public static bool operator ==(BoltConfig a, BoltConfig b) => a.Equals(b);
            public static bool operator !=(BoltConfig a, BoltConfig b) => !a.Equals(b);
        }

        public readonly string GroupId;
        public readonly bool AffectedByBreakerBoxes;
        public readonly BoltConfig[] Bolts;
        public readonly int Depth;
        public readonly PackedColor FillColor;

        public Config(string groupId, bool affectedByBreakerBoxes, BoltConfig[] bolts, int depth, PackedColor fillColor)
        {
            GroupId = groupId;
            AffectedByBreakerBoxes = affectedByBreakerBoxes;
            Bolts = bolts;
            Depth = depth;
            FillColor = fillColor;
        }

        public static Config Default => new("", false, DefaultBolts, -1000100, new PackedColor(24, 17, 9, 25));

        public bool Equals(Config other)
        {
            if (GroupId != other.GroupId || AffectedByBreakerBoxes != other.AffectedByBreakerBoxes ||
                Depth != other.Depth || FillColor != other.FillColor ||
                Bolts.Length != other.Bolts.Length) return false;
            for (int i = 0; i < Bolts.Length; i++)
                if (Bolts[i] != other.Bolts[i])
                    return false;
            return true;
        }

        public override bool Equals(object other) => other is Config config && Equals(config);
        public override int GetHashCode() => HashCode.Combine(GroupId, AffectedByBreakerBoxes, Depth, FillColor);
        public static bool operator ==(Config first, Config second) => first.Equals(second);
        public static bool operator !=(Config first, Config second) => !first.Equals(second);
    }

    public static readonly Config.BoltConfig[] DefaultBolts =
        { new(new PackedColor(252, 245, 121, 255), 1f, 0.1f), new(new PackedColor(140, 247, 226, 255), 1f, 0.1f) };

    [RuntimeTypeAlias("FrostHelper.CustomLightningRenderer+Edge")]
    public class Edge
    {
        public SceneEntityState Parent;
        public bool Visible = true;
        public Vector2 A, B, Min, Max;
        public uint SeedOffset { get; }
        private bool forcedInvisible;

        public bool ForcedInvisible
        {
            get => forcedInvisible;
            set
            {
                forcedInvisible = value;
                Visible = !value;
            }
        }

        public Edge(SceneEntityState parent, Vector2 a, Vector2 b)
        {
            Parent = parent;
            A = a;
            B = b;
            Min = new Vector2(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y));
            Max = new Vector2(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y));
            SeedOffset = unchecked((uint)(Hash(a) + Hash(b)));
        }

        public void Move(Vector2 move)
        {
            A += move;
            B += move;
            Min = new Vector2(Math.Min(A.X, B.X), Math.Min(A.Y, B.Y));
            Max = new Vector2(Math.Max(A.X, B.X), Math.Max(A.Y, B.Y));
        }

        public bool InView(ref IntRect view) => view.Left < Parent.X + Max.X && view.Right > Parent.X + Min.X &&
                                                view.Top < Parent.Y + Max.Y && view.Bottom > Parent.Y + Min.Y;
    }

    [RuntimeTypeAlias("FrostHelper.CustomLightningRenderer+Fill")]
    public class Fill
    {
        public IntRect R;
        public Fill(IntRect rect) => R = rect;
    }

    [RuntimeTypeAlias("FrostHelper.CustomLightningRenderer+ArbitraryFill")]
    public sealed class ArbitraryFill
    {
        public readonly Vector3[] Verts;
        public readonly IntRect Bounds;
        public SceneEntityState Parent;

        public IntRect RenderBounds =>
            new(Bounds.X + (int)Parent.X, Bounds.Y + (int)Parent.Y, Bounds.Width, Bounds.Height);

        public ArbitraryFill(SceneEntityState parent, Vector3[] verts)
        {
            Parent = parent;
            Verts = verts;
            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            foreach (var vertex in verts)
            {
                minX = Math.Min(minX, (int)vertex.X);
                minY = Math.Min(minY, (int)vertex.Y);
                maxX = Math.Max(maxX, (int)vertex.X);
                maxY = Math.Max(maxY, (int)vertex.Y);
            }

            Bounds = new(minX, minY, unchecked(maxX - minX), unchecked(maxY - minY));
        }
    }
}
