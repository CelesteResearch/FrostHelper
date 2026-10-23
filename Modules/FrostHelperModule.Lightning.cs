using System;
using Celeste.Mods.FrostHelper.Runtime.Lighting;
using Celeste.Mods.FrostHelper.Sessions;

namespace Celeste.Mods.FrostHelper.Modules
{
    public sealed partial class FrostHelperModule : IFrostLightningEnvironment
    {
        public override Type SessionType => typeof(FrostHelperSession);
        public FrostHelperSession Session => (FrostHelperSession)_Session;
        private FrostLightningColors lightning;
        private FrostLightningHooks lightningHooks;
        private FrostArbitraryLightningHooks arbitraryLightningHooks;

        public FrostLightningColors Lightning =>
            lightning ??= new FrostLightningColors(SourceEngine, Colors, () => Session);

        private void LoadLightningColors()
        {
            lightningHooks = new FrostLightningHooks(SourceEngine, Lightning);
            try
            {
                arbitraryLightningHooks = new FrostArbitraryLightningHooks(SourceEngine);
            }
            catch
            {
                lightningHooks.Dispose();
                lightningHooks = null;
                throw;
            }
        }

        private void UnloadLightningColors()
        {
            arbitraryLightningHooks?.Dispose();
            arbitraryLightningHooks = null;
            lightningHooks?.Dispose();
            lightningHooks = null;
        }
    }
}
