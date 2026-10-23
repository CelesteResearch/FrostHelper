using Celeste.Core.Reflection.Runtime;

namespace Celeste.Mods.FrostHelper.Triggers.Stylegrounds
{
    [RuntimeTypeAlias("FrostHelper.StylegroundMoveTrigger+AfterDeathBehaviours")]
    public enum AfterDeathBehaviour
    {
        Reset,
        Stay,
        SnapToEnd
    }
}
