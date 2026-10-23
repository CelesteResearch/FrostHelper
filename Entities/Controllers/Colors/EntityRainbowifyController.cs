// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Modules.Runtime;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.FrostHelper.Components.Colors;
using Celeste.Mods.FrostHelper.Filtering.Entities;
using Celeste.Mods.FrostHelper.Modules;
using Celeste.Mods.FrostHelper.Runtime.Colors;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation.Presentation.Backdrops.Runtime;

namespace Celeste.Mods.FrostHelper.Entities.Controllers.Colors
{
    [CustomEntity("FrostHelper/EntityRainbowifyController")]
    [RuntimeTypeAlias("FrostHelper.EntityRainbowifyController")]
    [SceneTracked(false)]
    public sealed class EntityRainbowifyController : SceneEntityState
    {
        private readonly FrostEntityFilter filter;
        private readonly EntityRainbowifyHooks hooks;
        private List<BackdropPresentation> affectedBackdrops;
        internal bool All { get; }

        public static EntityRainbowifyController Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) => new(data, offset, level.EngineContext);

        public EntityRainbowifyController(EntityData data, Vector2 offset, ISourceEngineContext engine) : base(
            ((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
            if (!string.IsNullOrWhiteSpace(data.Attr("channel", "")))
                throw new NotSupportedException(
                    "FrostHelper rainbow channels require RainbowChannelController; this content port currently supports the default channel.");
            var module = ((ISourceEverestModulesEnvironment)engine).Modules.OfType<FrostHelperModule>().Single();
            hooks = EntityRainbowifyHooks.For(engine, module);
            string types = data.Attr("types", "");
            if (types == "all")
            {
                All = true;
                return;
            }

            filter = FrostEntityFilter.CreateFrom(types, false, new FrostEntityTypeResolver(engine),
                notify: module.Warn);
            Add(new AnyEntityAddedHook(hooks, OtherEntityAdded, true));
        }

        private void OtherEntityAdded(SceneEntityState entity, object scene)
        {
            if (filter.Matches(entity)) entity.Add(new Rainbowifier(hooks));
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            if (filter == null) return;
            affectedBackdrops = new();
            foreach (var backdrop in ((OriginalLevelSceneRoot)scene).Background.Backdrops)
                if (filter.MatchesBackdrop(backdrop))
                    affectedBackdrops.Add(backdrop);
        }

        public override void Render(ISourceRenderContext context)
        {
            base.Render(context);
            if (affectedBackdrops == null) return;
            foreach (var backdrop in affectedBackdrops) backdrop.Color = hooks.GetHue(Scene, backdrop.Position);
        }
    }
}
