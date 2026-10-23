// SPDX-License-Identifier: MIT

using System.Linq;
using System.Numerics;
using Celeste.Everest.Modules.Runtime;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.FrostHelper.Modules;

namespace Celeste.Mods.FrostHelper.Entities.Controllers.Colors
{
    public sealed partial class RainbowTilesetController
    {
        public static RainbowTilesetController Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data)
        {
            var module = ((ISourceEverestModulesEnvironment)level.EngineContext).Modules.OfType<FrostHelperModule>()
                .Single();
            return new RainbowTilesetController(data, offset, level.EngineContext, module.Spinners);
        }
    }
}
