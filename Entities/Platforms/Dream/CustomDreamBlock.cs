// SPDX-License-Identifier: MIT

using System;
using System.Linq;
using System.Numerics;
using Celeste.Core.Components.Animation;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Easing;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Modules.Runtime;
using Celeste.Everest.Tracking.Attributes;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Entities.Platforms.Dream;
using Celeste.Game.Execution.Levels;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Display;
using Celeste.Graphics.Rendering;
using Celeste.Mods.FrostHelper.Animation.Easing;
using Celeste.Mods.FrostHelper.Modules;
using Celeste.Mods.FrostHelper.Rendering.Dream;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation.Gameplay.Platforms.Foundation;

namespace Celeste.Mods.FrostHelper.Entities.Platforms.Dream
{
    [CustomEntity("FrostHelper/CustomDreamBlock")]
    [SceneTracked]
    [EverestTrackedAs(typeof(OriginalDreamBlockState))]
    [RuntimeTypeAlias("FrostHelper.CustomDreamBlockV2")]
    public class CustomDreamBlock : OriginalDreamBlockState
    {
        private readonly float moveSpeedMult;
        private readonly Func<float, float> easing;
        private static readonly int[][] DefaultFrames = { new[] { 3, 2, 1, 0 }, new[] { 1, 2 }, new[] { 2 } };
        private static readonly float[] DefaultParallax = { .3f, .55f, .8f };

        public float DashSpeed;
        public bool AllowRedirects;
        public bool AllowRedirectsInSameDir;
        public float SameDirectionSpeedMultiplier;
        public bool ConserveSpeed;
        public PackedColor ActiveBackColor;
        public PackedColor DisabledBackColor;
        public PackedColor ActiveLineColor;
        public PackedColor DisabledLineColor;

        protected override PackedColor RenderActiveBackColor => ActiveBackColor;
        protected override PackedColor RenderDisabledBackColor => DisabledBackColor;
        protected override PackedColor RenderActiveLineColor => ActiveLineColor;
        protected override PackedColor RenderDisabledLineColor => DisabledLineColor;

        public static CustomDreamBlock Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) => new(data, offset, level.EngineContext,
            level.SolidWorldFactory);

        public CustomDreamBlock(EntityData data, Vector2 offset, ISourceEngineContext engine,
            ISourceSolidWorldFactory worlds)
            : base(engine, data.Position + offset, data.Width, data.Height, data.FirstNodeNullable(offset),
                data.Bool("fastMoving"), data.Bool("oneUse"), data.Bool("below"), worlds)
        {
            // Unsupported variants are explicit, never silently replaced with vanilla behavior.
            if (data.Bool("old") || data.Bool("connected") || data.Has("particles"))
                throw new NotSupportedException(
                    "Frost custom dream block old/connected/custom particles require their own port.");
            var module = ((ISourceEverestModulesEnvironment)engine).Modules.OfType<FrostHelperModule>().Single();
            module.EnsureDreamHooks();
            Depth = data.Int("depth", Depth);
            ActiveBackColor = module.Colors.Read(data.Attr("activeBackColor", "Black"));
            DisabledBackColor = module.Colors.Read(data.Attr("disabledBackColor", "1f2e2d"));
            ActiveLineColor = module.Colors.Read(data.Attr("activeLineColor", "White"));
            DisabledLineColor = module.Colors.Read(data.Attr("disabledLineColor", "6a8480"));
            DashSpeed = data.Float("speed", 240f);
            AllowRedirects = data.Bool("allowRedirects");
            AllowRedirectsInSameDir = data.Bool("allowSameDirectionDash");
            SameDirectionSpeedMultiplier = data.Float("sameDirectionSpeedMultiplier", 1f);
            moveSpeedMult = data.Float("moveSpeedMult", 1f);
            easing = new FrostEasingResolver(((ISourceEasingEnvironment)engine).EasingArithmetic)
                .Resolve(data.Attr("moveEase", "SineInOut"));
            ConserveSpeed = data.Bool("conserveSpeed");
            var texture = ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas[
                data.Attr("particlePath", "objects/dreamblock/particles")];
            particleTextures = new AtlasEntry[]
            {
                texture.GetSubtexture(14, 0, 7, 7),
                texture.GetSubtexture(7, 0, 7, 7), texture.GetSubtexture(0, 0, 7, 7),
                texture.GetSubtexture(7, 0, 7, 7)
            };
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            SourcePlayerHasDreamDash = ((OriginalLevelSceneRoot)Scene).Session.Inventory.DreamDash;
            if (!SourcePlayerHasDreamDash || !node.HasValue) return;
            Remove(Get<SourceTweenState>());
            Vector2 start = Position;
            Vector2 end = node.Value;
            float duration = ((ISourceVectorEnvironment)SourceEngine).VectorArithmetic.Distance(start, end) /
                             (12f * moveSpeedMult);
            if (fastMoving) duration /= 3f;
            var tween = SourceTweenState.Create(SourceEngine.TweenPool, SourceTweenState.TweenMode.YoyoLooping,
                easing, duration, true);
            tween.OnUpdate = current =>
            {
                Vector2 to = Vector2Interpolation.Lerp(start, end, current.Eased,
                    ((ISourceScalarEnvironment)SourceEngine).ScalarArithmetic);
                if (Collidable) MoveTo(to);
                else MoveToNaive(to);
            };
            Add(tween);
            node = null;
        }

        protected override void RenderParticles(ISourceRenderContext context)
        {
            Vector2 camera = ((OriginalLevelSceneRoot)Scene).Camera.Position;
            int x = (int)X, y = (int)Y, width = (int)Width, height = (int)Height;
            foreach (var particle in particles)
            {
                Vector2 position = DreamParticleRendering.Wrap(camera * DefaultParallax[particle.Layer] +
                                                               particle.Position, x, y, width, height);
                if (!(position.X >= x + 2.0) || !(position.Y >= y + 2.0) ||
                    !(position.X < x + width - 2.0) || !(position.Y < y + height - 2.0)) continue;
                int[] frames = DefaultFrames[particle.Layer];
                int frame = DreamParticleRendering.Frame(particle.TimeOffset, animTimer, frames);
                SourceAtlasDrawing.DrawCentered(context, this, particleTextures[frame],
                    position + SourceShake, particle.Color);
            }
        }
    }
}
