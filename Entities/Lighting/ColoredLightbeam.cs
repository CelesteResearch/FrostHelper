// SPDX-License-Identifier: MIT

using System.Linq;
using System.Numerics;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Modules.Runtime;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Environment.Ridge;
using Celeste.Mods.FrostHelper.Modules;

namespace Celeste.Mods.FrostHelper.Entities.Lighting
{
    [CustomEntity("FrostHelper/ColoredLightbeam")]
    public sealed class ColoredLightbeam : OriginalLightBeamState
    {
        public float ParallaxAmount;

        public static ColoredLightbeam Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) => new(data, offset, level.EngineContext);

        public ColoredLightbeam(EntityData data, Vector2 offset, ISourceEngineContext engine)
            : base(data, offset, engine)
        {
            var module = ((ISourceEverestModulesEnvironment)engine).Modules.OfType<FrostHelperModule>().Single();
            BeamColor = module.Colors.Read(data.Attr("color", "ccffff"));
            ParallaxAmount = data.Float("parallaxAmount");
        }

        public override void Render(ISourceRenderContext context)
        {
            Vector2 previous = Position;
            if (ParallaxAmount != 0f)
            {
                Vector2 cameraCenter = ((OriginalLevelSceneRoot)Scene).Camera.Position + new Vector2(160, 90);
                Position += (Position - cameraCenter) * ParallaxAmount;
            }

            base.Render(context);
            Position = previous;
        }
    }
}
