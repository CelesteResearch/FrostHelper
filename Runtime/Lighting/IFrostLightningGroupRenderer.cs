using Celeste.Mods.FrostHelper.Sessions.Lighting;

namespace Celeste.Mods.FrostHelper.Runtime.Lighting
{
    public interface IFrostLightningGroupRenderer
    {
        string GroupId { get; }
        void Apply(LightningGroupChange change);
    }
}
