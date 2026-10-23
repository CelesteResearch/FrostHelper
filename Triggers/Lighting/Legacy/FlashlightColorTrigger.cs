using System.Numerics;
using Celeste.Core.Scheduling;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;

namespace Celeste.Mods.FrostHelper.Triggers.Lighting.Legacy
{
    [CustomEntity("coloredlights/flashlightColorTrigger")]
    [RuntimeTypeAlias("ColoredLights.FlashlightColorTrigger")]
    public sealed class FlashlightColorTrigger : Lighting.FlashlightColorTrigger
    {
        public FlashlightColorTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(engine, data, offset)
        {
        }
    }
}
