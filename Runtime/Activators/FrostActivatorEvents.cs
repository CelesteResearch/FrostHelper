#nullable enable

using System;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Triggers.Foundation;
using MonoMod.ModInterop;

namespace Celeste.Mods.FrostHelper.Runtime.Activators
{
    [ModExportName("FrostHelper.Events")]
    public sealed class FrostActivatorEvents
    {
        private readonly FrostActivatorServices services;
        public FrostActivatorEvents(FrostActivatorServices services) => this.services = services;

        public IDisposable OnActivatorActivate(string modName,
            Action<SourceTriggerEntityState, SourceTriggerEntityState, RoomPlayerSession?> handler)
            => services.Subscribe(modName, handler);
    }
}
