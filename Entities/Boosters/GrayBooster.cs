using System.Linq;
using System.Numerics;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Modules.Runtime;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Mods.FrostHelper.Modules;
using Celeste.Mods.FrostHelper.Runtime.Boosters;
using Celeste.Scene.Tracking.Attributes;

namespace Celeste.Mods.FrostHelper.Entities.Boosters
{
    [CustomEntity("FrostHelper/GrayBooster")]
    [SceneTracked(false)]
    [RuntimeTypeAlias("FrostHelper.Entities.Boosters.GrayBooster")]
    public class GrayBooster : GenericCustomBooster
    {
        public GrayBooster(EntityData data, Vector2 offset, ISourceEngineContext engine)
            : this(((ISourceEverestModulesEnvironment)engine).Modules.OfType<FrostHelperModule>().Single().Boosters,
                data, offset)
        {
        }

        public GrayBooster(FrostBoosterRuntime runtime, EntityData data, Vector2 offset)
            : base(runtime, data, offset)
        {
            BoostTime = EntityAttributeReader.Float(data.Values, "boostTime", 0f);
        }

        public override bool CanFastbubble() => false;
    }
}
