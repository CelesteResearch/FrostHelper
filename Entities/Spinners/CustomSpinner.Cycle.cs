using Celeste.Core.Rendering.Diagnostics;

namespace Celeste.Mods.FrostHelper.Entities.Spinners
{
    public partial class CustomSpinner : ISourceOptionalCycleHitboxOffset
    {
        float? ISourceOptionalCycleHitboxOffset.CycleHitboxOffset => Controller.NoCycles ? null : offset;
    }
}
