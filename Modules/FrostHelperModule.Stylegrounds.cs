using Celeste.Mods.FrostHelper.Runtime.Stylegrounds;

namespace Celeste.Mods.FrostHelper.Modules
{
    public sealed partial class FrostHelperModule
    {
        private StylegroundPositions stylegroundPositions;

        private void LoadStylegroundPositions()
        {
            stylegroundPositions = StylegroundPositions.For(SourceEngine);
            stylegroundPositions.Load();
        }

        private void UnloadStylegroundPositions()
        {
            stylegroundPositions?.Dispose();
            stylegroundPositions = null;
        }
    }
}
