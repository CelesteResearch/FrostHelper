using System;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Actors.Foundation;
using MonoMod.ModInterop;

namespace Celeste.Mods.FrostHelper.Runtime.Springs
{
    [ModImportName("GravityHelper")]
    public sealed class FrostSpringGravity
    {
        public Action<string> RegisterModSupportBlacklist;
        public Func<bool> IsPlayerInverted;
        public Func<SourceActorEntityState, bool> IsActorInverted;
        public Action<RoomPlayerSession, float> InvertedSuperBounce;
        public float InvertIfPlayerInverted(float value) => IsPlayerInverted?.Invoke() ?? false ? -value : value;

        public void SuperBounce(RoomPlayerSession player, float fromY)
        {
            if (IsPlayerInverted?.Invoke() ?? false) InvokeInvertedBounce(player, fromY);
            else player.SuperBounce(fromY);
        }

        public void InvokeInvertedBounce(RoomPlayerSession player, float fromY)
        {
            if (RegisterModSupportBlacklist == null) return;
            if (InvertedSuperBounce == null)
                throw new InvalidOperationException(
                    "GravityHelper is loaded, but its inverted spring bounce capability is missing.");
            InvertedSuperBounce(player, fromY);
        }
    }
}
