// SPDX-License-Identifier: MIT

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Celeste.Core.Scheduling;
using Celeste.Mods.FrostHelper.Triggers.Movement;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Movement;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.FrostHelper.Runtime.Movement
{
    public sealed class NoMovementHooks : IDisposable
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, NoMovementHooks> owners = new();
        private readonly ISourceEngineContext engine;
        private Hook normal;

        private NoMovementHooks(ISourceEngineContext engine)
        {
            this.engine = engine;
        }

        public static NoMovementHooks Load(ISourceEngineContext engine)
        {
            var owner = owners.GetValue(engine, key => new(key));
            if (owner.normal == null)
                owner.normal = new Hook(
                    typeof(NormalSequence).GetMethod("UpdateCore", BindingFlags.NonPublic | BindingFlags.Instance),
                    (Func<Func<NormalSequence, int>, NormalSequence, int>)owner.UpdateNormal);
            return owner;
        }

        private int UpdateNormal(Func<NormalSequence, int> original, NormalSequence sequence)
        {
            var player = (sequence.World as RoomPlayerWorld)?.SourcePlayer;
            if (player != null && ReferenceEquals(sequence.EngineContext, engine) &&
                NoMovementTrigger.IsMovementDisabled(player.Scene))
            {
                int moveX = player.Data.MoveX;
                player.Data.MoveX = 0;
                int moveY = player.Input.MoveY.Value;
                player.Input.MoveX.Value = 0;
                player.Input.MoveY.Value = 0;
                int result = original(sequence);
                player.Data.MoveX = moveX;
                player.Input.MoveY.Value = moveY;
                player.Input.MoveX.Value = moveX;
                return result;
            }

            return original(sequence);
        }

        public static void Unload(ISourceEngineContext engine)
        {
            if (owners.TryGetValue(engine, out var owner)) owner.Dispose();
        }

        public void Dispose()
        {
            normal?.Dispose();
            normal = null;
        }
    }
}
