// SPDX-License-Identifier: MIT

using System.Numerics;
using System.Linq;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Runtime;
using Celeste.Mods.FrostHelper.Modules;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Mods.FrostHelper.Runtime.Boosters;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation;

namespace Celeste.Mods.FrostHelper.Entities.Boosters
{
    [CustomEntity("FrostHelper/BlueBooster")]
    [SceneTracked]
    [global::Celeste.Core.Reflection.Runtime.RuntimeTypeAlias("FrostHelper.Entities.Boosters.BlueBooster")]
    public class BlueBooster : GenericCustomBooster
    {
        public BlueBooster(EntityData data, Vector2 offset, ISourceEngineContext engine) : this(Resolve(engine), data,
            offset)
        {
        }

        public BlueBooster(FrostBoosterRuntime runtime, EntityData data, Vector2 offset) : base(runtime, data, offset)
        {
        }

        private static FrostBoosterRuntime Resolve(ISourceEngineContext engine) =>
            ((ISourceEverestModulesEnvironment)engine).Modules.OfType<FrostHelperModule>().Single().Boosters;

        public override void HandleDashRefill(RoomPlayerSession player)
        {
        }
    }
}
