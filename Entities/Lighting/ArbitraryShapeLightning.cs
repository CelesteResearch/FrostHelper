using System;
using System.Linq;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.LinearAlgebra;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Modules.Runtime;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Execution.Levels;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Collision.Components;
using Celeste.Mods.FrostHelper.Collider.Shapes;
using Celeste.Mods.FrostHelper.Geometry.Triangulation;
using Celeste.Mods.FrostHelper.Modules;
using Celeste.Mods.FrostHelper.Runtime.Lighting;

namespace Celeste.Mods.FrostHelper.Entities.Lighting
{
    [CustomEntity("FrostHelper/ArbitraryShapeLightning")]
    [SceneTracked(false)]
    [RuntimeTypeAlias("FrostHelper.ArbitraryShapeLightning")]
    public sealed class ArbitraryShapeLightning : SceneEntityState
    {
        public CustomLightningRenderer.ArbitraryFill Vertices;
        public CustomLightningRenderer.Edge[] Edges;
        public bool Fill;
        private CustomLightningRenderer renderer;
        private readonly CustomLightningRenderer.Config config;
        private readonly ISourceEngineContext engine;
        public bool AffectedByBreakerBoxes => config.AffectedByBreakerBoxes;

        public ArbitraryShapeLightning(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration,
                SourceVector2Math.Add(data.Position, offset))
        {
            this.engine = engine;
            var colors = ((ISourceEverestModulesEnvironment)engine).Modules.OfType<FrostHelperModule>().Single().Colors;
            Fill = data.Bool("fill", true);
            Depth = data.Int("depth", -1000100);
            config = new CustomLightningRenderer.Config(data.Attr("group", ""),
                data.Bool("affectedByLightningBoxes", false), FrostLightningBoltReader.Read(data, colors), Depth,
                colors.Read(data.Attr("fillColor", "18110919")));
            if (Fill)
                Vertices = new CustomLightningRenderer.ArbitraryFill(this,
                    ArbitraryShapeFill.FromNodes(data, SourceVector2Math.Negate(data.Position)));
            var nodes = new Vector2[data.Nodes.Length];
            for (int i = 0; i < nodes.Length; i++) nodes[i] = SourceVector2Math.Add(data.Nodes[i], offset);
            Edges = new CustomLightningRenderer.Edge[nodes.Length + (Fill ? 1 : 0)];
            for (int i = 1; i < nodes.Length; i++)
                Edges[i] = new CustomLightningRenderer.Edge(this, SourceVector2Math.Subtract(nodes[i - 1], Position),
                    SourceVector2Math.Subtract(nodes[i], Position));
            Edges[0] = new CustomLightningRenderer.Edge(this, Vector2.Zero,
                SourceVector2Math.Subtract(nodes[0], Position));
            if (Fill)
                Edges[Edges.Length - 1] = new CustomLightningRenderer.Edge(this,
                    SourceVector2Math.Subtract(nodes[nodes.Length - 1], Position), Vector2.Zero);
            var collisionPoints = new Vector2[nodes.Length + 1];
            collisionPoints[0] = Position;
            Array.Copy(nodes, 0, collisionPoints, 1, nodes.Length);
            Collider = new ShapeHitbox(collisionPoints) { Fill = Fill };
            Add(new PlayerColliderComponentState(OnPlayer, Collider));
        }

        public void OnPlayer(RoomPlayerSession player)
        {
            if (!player.Dead) player.Die(Vector2.UnitX, false, true);
        }

        private CustomLightningRenderer GetOrAddRenderer(object scene) =>
            renderer ??= CustomLightningRenderer.GetOrCreate(engine, scene, config);

        public override void Added(object scene)
        {
            base.Added(scene);
            var target = GetOrAddRenderer(scene);
            foreach (var edge in Edges) target.Add(edge);
            if (Fill) target.Add(Vertices);
        }

        public override void Removed(object scene)
        {
            base.Removed(scene);
            var target = GetOrAddRenderer(scene);
            foreach (var edge in Edges) target.Remove(edge);
            if (Vertices != null) target.Remove(Vertices);
        }

        public void Shatter()
        {
            if (Scene == null) return;
            var particles = ((OriginalLevelSceneRoot)Scene).ParticlesFG;
            var type = ((IOriginalGameplayProcess)engine).ParticleTypes.Lightning.P_Shatter;
            var vectors = ((ISourceVectorEnvironment)engine).VectorArithmetic;
            foreach (var edge in Edges)
            {
                var position = edge.A;
                while (position != edge.B)
                {
                    position = vectors.Approach(position, edge.B, 4f);
                    particles.Emit(type, 1, SourceVector2Math.Add(Position, position), Vector2.One * 3f);
                }
            }
        }
    }
}
