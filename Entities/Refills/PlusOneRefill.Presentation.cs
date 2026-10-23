using System.Runtime.CompilerServices;
using Celeste.Game.Gameplay.Collectibles.Refills;

namespace Celeste.Mods.FrostHelper.Entities.Refills
{
    internal sealed partial class PlusOneRefill
    {
        private readonly IRefillPresentationBinding presentation;

        private readonly struct PresentationPhases(PlusOneRefill owner) : IRefillPresentationPhases
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void UpdateTimerAndGlow<TContext>(in TContext context)
                where TContext : struct, IRefillPresentationContext
                => RefillPresentation.UpdateTimerAndGlowFromSource(owner, in context, owner.level,
                    ref owner.respawnTimer, owner.respawnAction, in owner.glowSource, owner.particleColor);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void UpdateY() => RefillPresentation.UpdateY(owner.sprite, owner.flash, owner.bloom, owner.sine);

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void UpdateLightAndFlash<TContext>(in TContext context)
                where TContext : struct, IRefillPresentationContext
                => RefillPresentation.UpdateLighting(owner, in context, owner.sprite, owner.flash, owner.light,
                    owner.bloom);
        }
    }
}
