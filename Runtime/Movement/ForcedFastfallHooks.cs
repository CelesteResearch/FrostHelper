// SPDX-License-Identifier: MIT

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Celeste.Core.Scheduling;
using Celeste.Mods.FrostHelper.Triggers.Movement;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Movement;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.FrostHelper.Runtime.Movement
{
    public sealed class ForcedFastfallHooks : IDisposable
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, ForcedFastfallHooks> owners = new();
        [ThreadStatic] private static RoomPlayerSession activePlayer;
        private static Hook normalScope;
        private static int loadedOwners;
        private readonly ISourceEngineContext engine;
        private ILHook fallLimit;

        private ForcedFastfallHooks(ISourceEngineContext engine)
        {
            this.engine = engine;
        }

        public static ForcedFastfallHooks Load(ISourceEngineContext engine)
        {
            var owner = owners.GetValue(engine, key => new(key));
            if (owner.fallLimit != null) return owner;
            if (normalScope == null)
                normalScope = new Hook(
                    typeof(NormalSequence).GetMethod("UpdateCore", BindingFlags.NonPublic | BindingFlags.Instance),
                    (Func<Func<NormalSequence, int>, NormalSequence, int>)UpdateNormal);
            try
            {
                owner.fallLimit = new ILHook(typeof(NormalMotion).GetMethod(nameof(NormalMotion.UpdateFallLimit)),
                    owner.PatchFallLimit);
            }
            catch
            {
                if (loadedOwners == 0)
                {
                    normalScope.Dispose();
                    normalScope = null;
                }

                throw;
            }

            loadedOwners++;
            return owner;
        }

        private static int UpdateNormal(Func<NormalSequence, int> original, NormalSequence sequence)
        {
            var previous = activePlayer;
            activePlayer = (sequence.World as RoomPlayerWorld)?.SourcePlayer;
            try
            {
                return original(sequence);
            }
            finally
            {
                activePlayer = previous;
            }
        }

        private void PatchFallLimit(ILContext context)
        {
            var cursor = new ILCursor(context);
            if (!cursor.TryGotoNext(MoveType.After,
                    instruction => instruction.MatchLdfld<NormalMotionContext>(nameof(NormalMotionContext.MoveY))))
                throw new InvalidOperationException("Missing Normal fast-fall input read.");
            cursor.EmitDelegate<Func<int, int>>(ReadFastfallInput);
        }

        private int ReadFastfallInput(int original)
        {
            var player = activePlayer;
            return player != null && ReferenceEquals(player.EngineContext, engine) &&
                   ForcedFastfallTrigger.IsForcedFastfall(player.Scene)
                ? 1
                : original;
        }

        public static void Unload(ISourceEngineContext engine)
        {
            if (owners.TryGetValue(engine, out var owner)) owner.Dispose();
        }

        public void Dispose()
        {
            if (fallLimit == null) return;
            fallLimit.Dispose();
            fallLimit = null;
            if (--loadedOwners != 0) return;
            normalScope.Dispose();
            normalScope = null;
        }
    }
}
