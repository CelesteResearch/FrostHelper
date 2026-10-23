using Celeste.Mods.FrostHelper.Runtime.Lighting;

namespace Celeste.Mods.FrostHelper.Modules
{
    public sealed partial class FrostHelperModule : IFrostBloomEnvironment
    {
        private FrostBloomColors bloomColors;

        public FrostBloomColors BloomColors =>
            bloomColors ??= new FrostBloomColors(SourceEngine, Colors, () => Session);

        private void LoadBloomColors() => BloomColors.Load();
        private void UnloadBloomColors() => bloomColors?.Dispose();
    }
}
