// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Celeste.Core.Scheduling;
using Celeste.Game.Gameplay.Walls.Breakable;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Dash;
using Celeste.Simulation.Gameplay.Player.Movement.Wind;
using Celeste.Simulation.Gameplay.Player.State;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MonoMod.Utils;

namespace Celeste.Mods.FrostHelper.Runtime.Boosters
{
    public sealed class FrostBoosterHooks : IDisposable
    {
        private readonly FrostBoosterRuntime runtime;
        private readonly List<IDisposable> hooks = [];
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        public FrostBoosterHooks(FrostBoosterRuntime runtime)
        {
            this.runtime = runtime;
            try
            {
                hooks.Add(new ILHook(typeof(RoomPlayerSession).GetConstructors().Single(), PlayerStates));
                hooks.Add(new ILHook(typeof(RoomPlayerSession).GetMethod(nameof(RoomPlayerSession.OnBoundsH)),
                    il => ReplaceState(il, 5, false, 0)));
                hooks.Add(new ILHook(typeof(RoomPlayerSession).GetMethod(nameof(RoomPlayerSession.OnBoundsV)),
                    il => ReplaceState(il, 5, false, 0)));
                hooks.Add(new ILHook(
                    typeof(RoomPlayerSession).GetProperty(nameof(RoomPlayerSession.DashAttacking)).GetMethod,
                    il => ReplaceState(il, 5, false, 0)));
                hooks.Add(new ILHook(typeof(OriginalDashBlockState).GetMethod("OnDashed", PrivateInstance),
                    il => ReplaceState(il, 5, false, 1)));
                hooks.Add(new ILHook(typeof(PlayerWindMovement).GetMethod(nameof(PlayerWindMovement.Move)), WindState));
                hooks.Add(new ILHook(typeof(DashSequence).GetMethod(nameof(DashSequence.CallEvents)), DashEvents));
                hooks.Add(new ILHook(
                    typeof(DashSequence).GetMethod(nameof(DashSequence.Coroutine)).GetStateMachineTarget(),
                    il => Speed(il, typeof(DashSequence), false)));
                hooks.Add(new ILHook(
                    typeof(PlayerBoostSequence).GetMethod(nameof(PlayerBoostSequence.RedDashCoroutine))
                        .GetStateMachineTarget(), il => Speed(il, typeof(PlayerBoostSequence), true)));
                hooks.Add(new ILHook(typeof(RoomPlayerSession).GetMethod(nameof(RoomPlayerSession.SuperJump)),
                    SuperJump));
                hooks.Add(new ILHook(typeof(DashSequence).GetMethod(nameof(DashSequence.End)), ResetDash));
                hooks.Add(new ILHook(typeof(RoomPlayerSession).GetMethod(nameof(RoomPlayerSession.DieSourceBody)),
                    ResetDeath));
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private void PlayerStates(ILContext il)
        {
            var cursor = new ILCursor(il);
            while (cursor.TryGotoNext(MoveType.Before, i => i.OpCode == OpCodes.Ret))
            {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Action<RoomPlayerSession>>(player =>
                {
                    if (Owns(player)) runtime.ForPlayer(player);
                });
                cursor.Index++;
            }
        }

        private bool Owns(RoomPlayerSession player) => ReferenceEquals(player.EngineContext, runtime.Engine);

        private int State(int original, RoomPlayerSession player, bool boost) =>
            Owns(player) && runtime.TryGetPlayer(player, out var state) &&
            player.Machine.State == (boost ? state.BoostState : state.RedState)
                ? player.Machine.State
                : original;

        private void ReplaceState(ILContext il, int original, bool boost, int arg)
        {
            var cursor = new ILCursor(il);
            while (cursor.TryGotoNext(MoveType.After,
                       i => i.MatchLdcI4(original) && i.Previous.MatchCallvirt<SimulationStateMachine>("get_State")))
            {
                cursor.Emit(arg == 0 ? OpCodes.Ldarg_0 : OpCodes.Ldarg_1);
                cursor.EmitDelegate<Func<int, RoomPlayerSession, int>>((value, player) => State(value, player, boost));
            }
        }

        private void WindState(ILContext il)
        {
            var cursor = new ILCursor(il);
            cursor.GotoNext(MoveType.After, i => i.MatchLdcI4(4));
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldfld, typeof(PlayerWindMovement).GetField("world", PrivateInstance));
            cursor.Emit(OpCodes.Callvirt,
                typeof(RoomPlayerWorld).GetProperty(nameof(RoomPlayerWorld.SourcePlayer)).GetMethod);
            cursor.EmitDelegate<Func<int, RoomPlayerSession, int>>((value, player) => State(value, player, true));
        }

        private static RoomPlayerSession Player(DashSequence dash) => ((RoomPlayerWorld)dash.World).SourcePlayer;

        private bool CallEvents(DashSequence dash)
        {
            var player = Player(dash);
            return !Owns(player) || runtime.ShouldCallDashEvents(player);
        }

        private void DashEvents(ILContext il)
        {
            var cursor = new ILCursor(il);
            cursor.GotoNext(MoveType.After, i => i.MatchStfld<PlayerState>(nameof(PlayerState.CalledEvents)));
            var resume = cursor.Next;
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<DashSequence, bool>>(CallEvents);
            cursor.Emit(OpCodes.Brtrue, resume);
            cursor.Emit(OpCodes.Ret);
        }

        private void Speed(ILContext il, Type owner, bool red)
        {
            var cursor = new ILCursor(il);
            var ownerField = owner
                .GetMethod(red ? nameof(PlayerBoostSequence.RedDashCoroutine) : nameof(DashSequence.Coroutine))
                .GetStateMachineTarget().DeclaringType.GetField("<>4__this",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            while (cursor.TryGotoNext(MoveType.After, i => i.MatchLdcR4(240f)))
            {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldfld, ownerField);
                if (!red)
                    cursor.EmitDelegate<Func<float, DashSequence, float>>((speed, dash) =>
                        ReferenceEquals(dash.EngineContext, runtime.Engine) ? runtime.ConsumeDashSpeed(speed) : speed);
                else
                {
                    cursor.Emit(OpCodes.Ldfld, typeof(PlayerBoostSequence).GetField("player", PrivateInstance));
                    cursor.EmitDelegate<Func<float, RoomPlayerSession, float>>((speed, player) =>
                        Owns(player) && runtime.TryGetPlayer(player, out var state) &&
                        player.Machine.State == state.RedState
                            ? runtime.ConsumeDashSpeed(speed)
                            : speed);
                }
            }
        }

        private void SuperJump(ILContext il)
        {
            var cursor = new ILCursor(il);
            while (cursor.TryGotoNext(MoveType.After, i => i.MatchLdcR4(260f)))
            {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<float, RoomPlayerSession, float>>((speed, player) =>
                    Owns(player) ? runtime.ConsumeSuperJumpSpeed(speed) : speed);
            }
        }

        private void ResetDash(ILContext il)
        {
            var cursor = new ILCursor(il);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Action<DashSequence>>(dash =>
            {
                if (ReferenceEquals(dash.EngineContext, runtime.Engine)) runtime.ResetSpeed();
            });
        }

        private void ResetDeath(ILContext il)
        {
            var cursor = new ILCursor(il);
            cursor.GotoNext(MoveType.Before, i => i.MatchCall(typeof(RoomPlayerSession), "Stop"));
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Action<RoomPlayerSession>>(player =>
            {
                if (Owns(player)) runtime.ResetSpeed();
            });
        }

        public void Dispose()
        {
            for (int i = hooks.Count - 1; i >= 0; i--) hooks[i].Dispose();
            hooks.Clear();
        }
    }
}
