using Celeste.Mods.FrostHelper.Runtime.Lighting;

namespace Celeste.Mods.FrostHelper.Modules
{
    public sealed partial class FrostHelperModule : IFrostFlashlightEnvironment
    {
        private FrostFlashlightColors flashlights;

        public FrostFlashlightColors Flashlights => flashlights ??= new FrostFlashlightColors(SourceEngine,
            Colors, () => Session.FlashlightColor, value => Session.FlashlightColor = value);

        private void LoadFlashlightColors() => Flashlights.Load();
        private void UnloadFlashlightColors() => flashlights?.Dispose();
    }
}
