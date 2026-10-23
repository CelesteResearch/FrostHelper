using Celeste.Mods.FrostHelper.Runtime.Activators;

namespace Celeste.Mods.FrostHelper.Modules
{
    public sealed partial class FrostHelperModule : IFrostActivatorEnvironment
    {
        public FrostActivatorServices Activators { get; private set; } = null!;

        private void LoadActivators()
        {
            Activators = new FrostActivatorServices(SourceEngine);
            try
            {
                Activators.Load();
            }
            catch
            {
                Activators.Dispose();
                throw;
            }
        }

        private void UnloadActivators() => Activators?.Dispose();
    }
}
