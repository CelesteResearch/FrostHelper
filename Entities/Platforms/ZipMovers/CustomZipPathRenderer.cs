using Celeste.Game.Presentation.Rendering;
using Celeste.Graphics.Effects.Composition;
using System.Runtime.CompilerServices;
using Celeste.Graphics.Display;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Celeste.Core.Scheduling;
using Celeste.Core.Components;
using Celeste.Core.Components.Timing;
using Celeste.Core.Display;
using Celeste.Core.Geometry;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Mathematics.Easing;
using Celeste.Core.Mathematics.LinearAlgebra;
using Celeste.Core.Rendering;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Modules.Runtime;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Controls.Source;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Entities.Platforms.ZipMovers.Motion;
using Celeste.Game.Presentation.Platforms.ZipMovers;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Rendering;
using Celeste.Graphics.Rendering.Drawing;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation.Audio.Sources;
using Celeste.Simulation.Gameplay.Environment.Core;
using Celeste.Simulation.Gameplay.Player.Intro;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Platforms.Collision;
using Celeste.Simulation.Presentation.PostProcessing.Bloom;
using Celeste.Simulation.Presentation.Lighting;
using Celeste.Simulation.Presentation.Particles.Definitions;
using Celeste.Mods.FrostHelper.Modules;
using Celeste.Mods.FrostHelper.Runtime.Rendering;
using Celeste.Mods.FrostHelper.Runtime.ZipMovers;

namespace Celeste.Mods.FrostHelper.Entities.Platforms.ZipMovers
{
    [SceneTracked(false)]
    [RuntimeTypeAlias("FrostHelper.CustomZipMover+ZipMoverPathRenderer")]
    internal sealed class CustomZipPathRenderer : SceneEntityState
    {
        private sealed class Pending
        {
            public CustomZipPathRenderer Value;
        }

        private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<object, Pending>
            PendingControllers =
                new();

        private readonly ISourceEngineContext engine;
        private readonly FullscreenRenderTargetPool targets;
        private readonly Action drawPaths;
        private readonly Action beginGameplay;
        private ISourceRenderContext renderContext;
        private List<object> zippers;

        public CustomZipPathRenderer(ISourceEngineContext engine) : base(((ISourceEntitySourceDataEnvironment)engine)
            .EntitySourceDataRegistration)
        {
            this.engine = engine;
            targets = CustomZipSpriteSource.PathTargets(engine);
            drawPaths = DrawAllPaths;
            beginGameplay = () => GameplayRendererState.Begin(renderContext);
            Depth = 5000;
            AddTag(((Celeste.Game.Execution.Startup.IOriginalGameTagsEnvironment)engine).Tags.Persistent.Value);
        }

        public static CustomZipPathRenderer Find(object scene)
        {
            var scheduler = ((ISourceScene)scene).SourceScene;
            return scheduler.Entities.GetTrackedEntity<CustomZipPathRenderer>(scheduler.Catalog
                .Find(typeof(CustomZipPathRenderer)).RegistryKey);
        }

        public static CustomZipPathRenderer Ensure(ISourceEngineContext engine, object scene)
        {
            var tracked = Find(scene);
            var pending = PendingControllers.GetValue(scene, _ => new Pending());
            if (tracked == null && pending.Value == null)
            {
                pending.Value = new CustomZipPathRenderer(engine);
                ((ISourceScene)scene).SourceScene.QueueSourceEntity(pending.Value);
            }

            return tracked ?? pending.Value;
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            PendingControllers.GetValue(scene, _ => new Pending()).Value = null;
        }

        private static Vector2 From(CustomZipMover zipper) =>
            zipper.start + new Vector2(zipper.Width / 2, zipper.Height / 2);

        private static Vector2 To(CustomZipMover zipper) =>
            zipper.target + new Vector2(zipper.Width / 2, zipper.Height / 2);

        public void CreateSparks(CustomZipMover zipper)
        {
            var from = From(zipper);
            var to = To(zipper);
            var vectors = ((ISourceVectorEnvironment)engine).VectorArithmetic;
            var add = Vector2Geometry.Perpendicular(vectors.SafeNormalize(from - to, 5));
            float angle = Vector2Geometry.Angle(from - to);
            ZipMoverDrawing.Sparks(this, engine, from, to, add, angle + .3926991f, angle - .3926991f,
                angle + 3.14159274f - .3926991f, angle + 3.14159274f + .3926991f);
        }

        public override void Render(ISourceRenderContext context)
        {
            var scheduler = ((ISourceScene)Scene).SourceScene;
            zippers = scheduler.Entities.GetTrackedEntities(scheduler.Catalog.Find(typeof(CustomZipMover)).RegistryKey);
            if (zippers.Count == 0) return;
            renderContext = context;
            var level = (OriginalLevelSceneRoot)Scene;
            targets.Render(context, level.Buffers.Gameplay.Width, level.Buffers.Gameplay.Height, level.Camera.Position,
                drawPaths, beginGameplay);
            foreach (CustomZipMover zipper in zippers)
            {
                if (zipper._outlineColor != PackedColor.Transparent)
                {
                    var rectangle = new IntRect((int)(zipper.X + zipper.Shake.X - 1f),
                        (int)(zipper.Y + zipper.Shake.Y - 1f), (int)zipper.Width + 2, (int)zipper.Height + 2);
                    if (FrostCameraCull.IsRectangleVisible(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height,
                            4,
                            level.Camera))
                        SourcePrimitiveDrawing.Rect((ISourceDrawScope)context, rectangle, zipper._outlineColor);
                }
            }
        }

        private void DrawAllPaths()
        {
            var draw = ((ISourceDrawScope)renderContext).Draw;
            var oldPixel = draw.Pixel;
            var camera = ((OriginalLevelSceneRoot)Scene).Camera;
            try
            {
                foreach (CustomZipMover zipper in zippers)
                {
                    var from = From(zipper);
                    var to = To(zipper);
                    draw.Pixel = zipper.CurrentSource.Pixel();
                    int ax = (int)from.X, ay = (int)from.Y, bx = (int)to.X, by = (int)to.Y;
                    if (ax > bx) (ax, bx) = (bx, ax);
                    if (ay > by) (ay, by) = (by, ay);
                    if (FrostCameraCull.IsRectangleVisible(ax, ay, bx - ax, by - ay, 4, camera))
                        DrawCogs(renderContext, zipper, Vector2.Zero);
                }
            }
            finally
            {
                draw.Pixel = oldPixel;
            }
        }

        internal void DrawCogs(ISourceRenderContext context, CustomZipMover zipper, Vector2 offset,
            PackedColor? colorOverride = null)
        {
            if (!zipper.drawLine) return;
            var from = From(zipper);
            var to = To(zipper);
            var vectors = ((ISourceVectorEnvironment)engine).VectorArithmetic;
            var direction = vectors.SafeNormalize(to - from);
            var perpendicular = Vector2Geometry.Perpendicular(direction);
            var a = perpendicular * 3;
            var b = -perpendicular * 4;
            ZipMoverDrawing.RopeLines(context, this, vectors, from + a, from + b, to + a, to + b, direction,
                perpendicular,
                vectors.Length(to - from), zipper.percent * 3.14159274f * 8f, offset, zipper.ropeColor,
                zipper.ropeLightColor, colorOverride);
            float rotation = zipper.percent * 3.14159274f * 2;
            var cog = zipper.CurrentSource.Cog();
            var color = zipper.Rainbow ? zipper.Module.Spinners.GetHue(Scene, from + offset) : zipper.tint;
            SourceAtlasDrawing.DrawCentered(context, this, cog, from + offset, colorOverride ?? color, 1, rotation);
            color = zipper.Rainbow ? zipper.Module.Spinners.GetHue(Scene, to + offset) : zipper.tint;
            SourceAtlasDrawing.DrawCentered(context, this, cog, to + offset, colorOverride ?? color, 1, rotation);
        }
    }
}
