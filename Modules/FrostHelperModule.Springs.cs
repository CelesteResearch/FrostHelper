using Celeste.Mods.FrostHelper.Runtime.Springs;

namespace Celeste.Mods.FrostHelper.Modules
{
    public sealed partial class FrostHelperModule
    {
        private FrostSpringServices springs;

        private void LoadSprings()
        {
            springs = new FrostSpringServices(SourceEngine, Colors, () => Session.NoClimbTimer,
                value => Session.NoClimbTimer = value);
            try
            {
                springs.EnsureHooks();
            }
            catch
            {
                springs.Dispose();
                springs = null;
                throw;
            }
        }

        private void UnloadSprings()
        {
            springs?.Dispose();
            springs = null;
        }
    }
}
