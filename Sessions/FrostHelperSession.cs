using System.Collections.Generic;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Persistence;
using Celeste.Mods.FrostHelper.Sessions.Lighting;

namespace Celeste.Mods.FrostHelper.Sessions
{
    [RuntimeTypeAlias("FrostHelper.FrostHelperSession")]
    public partial class FrostHelperSession : EverestModuleSession
    {
        public float NoClimbTimer { get; set; }
        public string LightningColorA { get; set; }
        public string LightningColorB { get; set; }
        public string LightningFillColor { get; set; }
        public float? LightningFillColorMultiplier { get; set; }
        public Dictionary<string, LightningGroupChange> ArbitraryLightningGroupChanges { get; set; } = new();
    }
}
