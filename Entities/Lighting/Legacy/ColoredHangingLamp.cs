// SPDX-License-Identifier: MIT

using System.Numerics;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;

namespace Celeste.Mods.FrostHelper.Entities.Lighting.Legacy
{
    [CustomEntity("coloredlights/hanginglamp")]
    [Celeste.Core.Reflection.Runtime.RuntimeTypeAlias("ColoredLights.ColoredHangingLamp")]
    public sealed class ColoredHangingLamp : Lighting.ColoredHangingLamp
    {
        public new static ColoredHangingLamp Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) => new(data, offset, level.EngineContext);

        public ColoredHangingLamp(EntityData data, Vector2 offset, ISourceEngineContext engine)
            : base(data, offset, engine)
        {
        }
    }
}
