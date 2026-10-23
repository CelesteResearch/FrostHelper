using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.FrostHelper.Runtime.Expressions;
using Celeste.Mods.FrostHelper.Runtime.Stylegrounds;
using Celeste.Scene.Entities;
using Celeste.Simulation.Presentation.Backdrops.Parallax;
using Celeste.Simulation.Presentation.Backdrops.Runtime;

namespace Celeste.Mods.FrostHelper.Entities.Controllers.Stylegrounds
{
    [CustomEntity("FrostHelper/DontUpdateInvisibleStylegroundsController")]
    [RuntimeTypeAlias("FrostHelper.DontUpdateInvisibleStylegroundsController")]
    public sealed class DontUpdateInvisibleStylegroundsController : SceneEntityState
    {
        private readonly ISourceEngineContext engine;
        private readonly Type[] affectedTypes;
        private readonly bool bg;

        public DontUpdateInvisibleStylegroundsController(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
            this.engine = engine;
            affectedTypes = FrostConditionRuntime.For(engine).GetTypes(data.Attr("types", ""));
            bg = data.Bool("bg", false);
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            var level = scene as OriginalLevelSceneRoot;
            WrapBackdrops(level.Foreground.Backdrops);
            if (bg) WrapBackdrops(level.Background.Backdrops);
            RemoveSelf();
        }

        private void WrapBackdrops(List<BackdropPresentation> backdrops)
        {
            for (int i = backdrops.Count - 1; i >= 0; i--)
            {
                var backdrop = backdrops[i];
                Type type = backdrop.GetType();
                bool affected = false;
                foreach (var candidate in affectedTypes)
                    if (ReferenceEquals(candidate, type))
                    {
                        affected = true;
                        break;
                    }

                if (!affected) continue;
                if (backdrop is ParallaxBackdropPresentation parallax)
                    InvisibleStylegroundHooks.Load(engine).Wrap(parallax);
                else backdrops[i] = new InvisibleStylegroundWrapper(backdrop);
            }
        }
    }
}
