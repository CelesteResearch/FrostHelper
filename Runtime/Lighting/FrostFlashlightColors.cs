using System;
using Celeste.Core.Display;
using Celeste.Core.Scheduling;
using Celeste.Everest.Runtime.Events.Player;
using Celeste.Mods.FrostHelper.Color;
using Celeste.Simulation;

namespace Celeste.Mods.FrostHelper.Runtime.Lighting
{
    public sealed class FrostFlashlightColors : IDisposable
    {
        private readonly ISourceEngineContext engine;
        private readonly Func<PackedColor?> getSessionColor;
        private readonly Action<PackedColor?> setSessionColor;
        public ColorReader Colors { get; }

        public FrostFlashlightColors(ISourceEngineContext engine, ColorReader colors,
            Func<PackedColor?> getSessionColor, Action<PackedColor?> setSessionColor)
        {
            this.engine = engine;
            Colors = colors;
            this.getSessionColor = getSessionColor;
            this.setSessionColor = setSessionColor;
        }

        public void SetSessionColor(PackedColor color) => setSessionColor(color);
        public void Load() => EverestPlayerSourceEvents.OnSpawn += PlayerSpawn;

        private void PlayerSpawn(RoomPlayerSession player)
        {
            if (ReferenceEquals(player.EngineContext, engine))
                player.Light.Color = getSessionColor() ?? player.Light.Color;
        }

        public void Dispose() => EverestPlayerSourceEvents.OnSpawn -= PlayerSpawn;
    }
}
