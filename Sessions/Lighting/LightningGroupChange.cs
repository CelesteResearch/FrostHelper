using Celeste.Core.Display;
using Celeste.Core.Reflection.Runtime;

namespace Celeste.Mods.FrostHelper.Sessions.Lighting
{
    [RuntimeTypeAlias("FrostHelper.FrostHelperSession+ArbitraryLightningGroupChange")]
    public class LightningGroupChange
    {
        public PackedColor[] Colors { get; set; }
        public PackedColor? FillColor { get; set; }
        public float? FillColorMultiplier { get; set; }
        public int? Depth { get; set; }
    }
}
