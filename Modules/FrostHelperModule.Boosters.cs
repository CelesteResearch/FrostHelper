// SPDX-License-Identifier: MIT

using Celeste.Mods.FrostHelper.Runtime.Boosters;

namespace Celeste.Mods.FrostHelper.Modules
{
    public sealed partial class FrostHelperModule
    {
        private FrostBoosterRuntime boosters;
        private FrostBoosterHooks boosterHooks;

        internal FrostBoosterRuntime Boosters
        {
            get
            {
                EnsureBoosterHooks();
                return boosters;
            }
        }

        private void EnsureBoosterHooks()
        {
            if (boosters != null) return;
            boosters = new FrostBoosterRuntime(SourceEngine, Colors, Warn, () => Session);
            try
            {
                boosterHooks = new FrostBoosterHooks(boosters);
            }
            catch
            {
                boosters.Dispose();
                boosters = null;
                throw;
            }
        }

        private void UnloadBoosterHooks()
        {
            boosterHooks?.Dispose();
            boosterHooks = null;
            boosters?.Dispose();
            boosters = null;
        }
    }
}
