// SPDX-License-Identifier: MIT

using System;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Geometry;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Randomness;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Modules.Runtime;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Rendering;
using Celeste.Mods.FrostHelper.Modules;
using Celeste.Physics.Collision;
using Celeste.Scene.Entities;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Platforms.StaticMovers;
using Celeste.Simulation.Presentation.Lighting;

namespace Celeste.Mods.FrostHelper.Entities.Decoration.Wires
{
    [CustomEntity("FrostHelper/WireLamps = Load")]
    [Celeste.Core.Reflection.Runtime.RuntimeTypeAlias("FrostHelper.WireLamps")]
    public class WireLamps : SceneEntityState, ISourceEngineContextEntity
    {
        private static readonly PackedColor[] DefaultColors =
            [PackedColor.Red, PackedColor.Yellow, PackedColor.Blue, PackedColor.Green, PackedColor.Orange];

        private readonly ISourceEngineContext engine;
        internal AtlasEntry LampTexture;
        internal Sprite[] Sprites;
        public float Wobbliness;
        private bool beginNodeBroken;
        private float beginNodeYSpeed;
        private bool endNodeBroken;
        private float endNodeYSpeed;
        private bool updated;
        public VertexLightState[] Lights;
        public PackedColor Color;
        public SimpleCurve Curve;
        private readonly float sineX;
        private readonly float sineY;
        ISourceEngineContext ISourceEngineContextEntity.EngineContext => engine;

        public static WireLamps Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data)
        {
            foreach (var loaded in ((ISourceEverestModulesEnvironment)level.EngineContext).Modules)
                if (loaded is FrostHelperModule module)
                    return new WireLamps(level.EngineContext, module, data, offset);
            throw new InvalidOperationException("FrostHelper must be loaded before its wire lamps.");
        }

        public WireLamps(ISourceEngineContext engine, FrostHelperModule module, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
            this.engine = engine;
            var to = data.Nodes[0] + offset;
            Curve = new SimpleCurve(Position, to, Vector2.Zero);
            Depth = (data.Bool("above") ? -8500 : 2000) - 1;
            var random = ((ISourceRandomProcessEnvironment)engine).RandomProcess.Factory.CreateSeeded(
                (int)((ISourceExtremaEnvironment)engine).ExtremaArithmetic.Min(Position.X, to.X));
            var colors = module.Colors.ReadArray(data.Attr("colors")) ?? DefaultColors;
            Color = module.Colors.Read(data.Attr("wireColor", "595866"));
            sineX = SourceRandomMath.NextFloat(random, 4);
            sineY = SourceRandomMath.NextFloat(random, 4);
            Wobbliness = data.Float("wobbliness", 1);
            Lights = new VertexLightState[data.Int("lightCount", 3)];
            var alpha = data.Float("lightAlpha", 1);
            var start = data.Int("lightStartFade", 8);
            var end = data.Int("lightEndFade", 16);
            var path = data.Attr("lampSprite", "objects/FrostHelper/wireLamp");
            var atlas = ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas;
            var animated = !atlas.Has(path);
            if (animated) Sprites = new Sprite[Lights.Length];
            else LampTexture = atlas[path];
            for (var i = 0; i < Lights.Length; i++)
            {
                Add(Lights[i] = new VertexLightState(engine, colors[random.Next(0, colors.Length)], alpha, start, end));
                if (!animated) continue;
                var sprite = AtlasSpriteFactory.Create(engine, atlas, path);
                sprite.Color = Lights[i].Color;
                sprite.AddLoop("i", "", data.Float("frameDelay", .5f));
                sprite.Play("i", randomizeFrame: true);
                sprite.CenterOrigin();
                Add(Sprites[i] = sprite);
            }

            if (!data.Bool("attached")) return;
            SourceStaticMoverState begin = null;
            Add(begin = new SourceStaticMoverState
            {
                SolidChecker = solid => SourceEntityCollision.CheckRect(solid, EndpointRectangle(Curve.Begin)),
                OnMove = amount => Curve.Begin += amount,
                OnShake = amount => Curve.Begin += amount,
                OnDestroy = () =>
                {
                    beginNodeBroken = true;
                    begin.Platform.Collidable = false;
                    if (!updated) Raycast(ref Curve.Begin, ref beginNodeYSpeed, ref beginNodeBroken);
                },
                OnDisable = PreserveDisabledAttachment
            });
            SourceStaticMoverState finish = null;
            Add(finish = new SourceStaticMoverState
            {
                SolidChecker = solid => SourceEntityCollision.CheckRect(solid, EndpointRectangle(Curve.End)),
                OnMove = amount => Curve.End += amount,
                OnShake = amount => Curve.End += amount,
                OnDestroy = () =>
                {
                    endNodeBroken = true;
                    finish.Platform.Collidable = false;
                    if (!updated) Raycast(ref Curve.End, ref endNodeYSpeed, ref endNodeBroken);
                },
                OnDisable = PreserveDisabledAttachment
            });
        }

        // The original intentionally keeps the endpoint attached when the platform is disabled.
        private static void PreserveDisabledAttachment()
        {
        }

        private static IntRect EndpointRectangle(Vector2 point) => new((int)point.X, (int)point.Y, 1, 1);

        public override void Update()
        {
            updated = true;
            base.Update();
            var bounds = ((OriginalLevelSceneRoot)Scene).Bounds;
            if (beginNodeBroken) HandleNodeMove(ref Curve.Begin, ref beginNodeYSpeed, ref beginNodeBroken, bounds);
            if (endNodeBroken) HandleNodeMove(ref Curve.End, ref endNodeYSpeed, ref endNodeBroken, bounds);
        }

        private bool HandleNodeMove(ref Vector2 position, ref float speed, ref bool broken, IntRect bounds)
        {
            var scene = (OriginalLevelSceneRoot)Scene;
            speed = ScalarMath.Approach(speed, 160, 500 * engine.DeltaTime);
            var solid = SourceSceneCollision.FirstPoint<SourceSolidEntityState>(scene.SourceScene.Entities,
                "Celeste.Solid", position);
            if (solid != null)
            {
                if (solid is ISourceSolidTilesEntity) broken = false;
                return true;
            }

            position.Y += speed * engine.DeltaTime;
            if (position.Y <= bounds.Bottom + 16) return false;
            broken = false;
            return true;
        }

        private void Raycast(ref Vector2 position, ref float speed, ref bool broken)
        {
            if (Scene is not OriginalLevelSceneRoot scene) return;
            var bounds = scene.Bounds;
            while (!HandleNodeMove(ref position, ref speed, ref broken, bounds))
            {
            }
        }

        public override void Render(ISourceRenderContext context)
        {
            if (Scene is not OriginalLevelSceneRoot level) return;
            var wobble = new Vector2((float)Math.Sin(sineX + level.WindSineTimer * 2),
                (float)Math.Sin(sineY + level.WindSineTimer * 2.8f)) * 8;
            Curve.Control = ((ISourceVectorEnvironment)engine).VectorArithmetic.Divide(Curve.Begin + Curve.End, 2)
                            + new Vector2(0, 24) + wobble * Wobbliness;
            if (!IsCurveVisible(level)) return;
            var curves = ((ISourceCurveEnvironment)engine).CurveArithmetic;
            SourceSimpleCurveDrawing.Render(context, this, Curve, Vector2.Zero, Color, 16,
                curves, ((ISourceVectorEnvironment)engine).VectorArithmetic);
            for (var i = 1; i <= Lights.Length; i++)
            {
                var point = curves.GetPoint(Curve, i / (Lights.Length + 1f));
                Lights[i - 1].Position = point - Position;
                if (LampTexture != null)
                    SourceAtlasDrawing.DrawCentered(context, this, LampTexture, point, Lights[i - 1].Color);
                else Sprites[i - 1].Position = Lights[i - 1].Position;
            }

            base.Render(context);
        }

        private bool IsCurveVisible(OriginalLevelSceneRoot level)
        {
            var extrema = ((ISourceExtremaEnvironment)engine).ExtremaArithmetic;
            var left = extrema.Min(Curve.Begin.X, extrema.Min(Curve.Control.X, Curve.End.X));
            var right = extrema.Max(Curve.Begin.X, extrema.Max(Curve.Control.X, Curve.End.X));
            var top = extrema.Min(Curve.Begin.Y, extrema.Min(Curve.Control.Y, Curve.End.Y));
            var bottom = extrema.Max(Curve.Begin.Y, extrema.Max(Curve.Control.Y, Curve.End.Y));
            var x = left - level.Camera.Left;
            var y = top - level.Camera.Top;
            // Same four lane arithmetic as original CameraCullHelper's accelerated rectangle branch.
            return -(right - left) - x <= 4 && -(bottom - top) - y <= 4
                                            && x - level.Camera.Viewport.Width * level.Camera.Zoom <= 4
                                            && y - level.Camera.Viewport.Height * level.Camera.Zoom <= 4;
        }
    }
}
