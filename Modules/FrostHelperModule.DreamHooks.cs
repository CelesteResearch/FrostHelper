// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Celeste.Core.Mathematics;
using Celeste.Game.Execution.Processes;
using Celeste.Mods.FrostHelper.Entities.Platforms.Dream;
using Celeste.Scene.Tracking;
using Celeste.Physics.Collision;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Collision;
using Celeste.Simulation.Gameplay.Player.Movement;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.FrostHelper.Modules
{
    public sealed partial class FrostHelperModule
    {
        private readonly List<IDisposable> dreamHooks = new();
        private ConditionalWeakTable<RoomPlayerSession, DreamSpeedState> dreamSpeeds = new();

        private sealed class DreamSpeedState
        {
            public float LastSpeed;
        }

        internal void EnsureDreamHooks()
        {
            if (dreamHooks.Count != 0) return;
            try
            {
                dreamHooks.Add(new ILHook(DreamMethod("DreamDashBegin"), PatchDreamDashBegin));
                dreamHooks.Add(new Hook(DreamMethod("DreamDashUpdate"),
                    (Func<Func<RoomPlayerSession, int>, RoomPlayerSession, int>)UpdateDreamDash));
                dreamHooks.Add(new Hook(DreamMethod("DreamDashEnd"),
                    (Action<Action<RoomPlayerSession>, RoomPlayerSession>)EndDreamDash));
            }
            catch
            {
                UnloadDreamHooks();
                throw;
            }
        }

        private static MethodInfo DreamMethod(string name)
            => typeof(RoomPlayerSession).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)
               ?? throw new MissingMethodException(typeof(RoomPlayerSession).FullName, name);

        private void UnloadDreamHooks()
        {
            for (int i = dreamHooks.Count - 1; i >= 0; i--) dreamHooks[i].Dispose();
            dreamHooks.Clear();
            dreamSpeeds = new ConditionalWeakTable<RoomPlayerSession, DreamSpeedState>();
        }

        private void PatchDreamDashBegin(ILContext il)
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.Before,
                    instruction => instruction.MatchStfld<NormalMotionState>(nameof(NormalMotionState.Speed))))
                throw new InvalidOperationException("Shared DreamDashBegin speed assignment was not found.");
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<Vector2, RoomPlayerSession, Vector2>>(GetDreamDashSpeed);
        }

        private CustomDreamBlock FirstDreamBlock(RoomPlayerSession player, Vector2 position)
        {
            if (!ReferenceEquals(player.EngineContext, SourceEngine) || player.Scene is not ISourceScene scene)
                return null;
            foreach (object entry in scene.SourceScene.Entities.GetTrackedEntities(typeof(CustomDreamBlock)))
            {
                var block = (CustomDreamBlock)entry;
                if (block.Collidable && !ReferenceEquals(player, block) &&
                    SourceEntityCollision.CheckAt(player, block, position)) return block;
            }

            return null;
        }

        private Vector2 GetDreamDashSpeed(Vector2 original, RoomPlayerSession player)
        {
            if (!ReferenceEquals(player.EngineContext, SourceEngine)) return original;
            var block = FirstDreamBlock(player, player.Position + Vector2.UnitX * Math.Sign(player.Speed.X)) ??
                        FirstDreamBlock(player, player.Position + Vector2.UnitY * Math.Sign(player.Speed.Y));
            return block is { ConserveSpeed: true } ? player.Speed * Math.Sign(block.DashSpeed) : original;
        }

        private int UpdateDreamDash(Func<RoomPlayerSession, int> original, RoomPlayerSession player)
        {
            var block = FirstDreamBlock(player, player.Position);
            if (block != null)
            {
                var state = dreamSpeeds.GetValue(player, static _ => new DreamSpeedState());
                if (!block.ConserveSpeed && state.LastSpeed != block.DashSpeed)
                {
                    player.Speed = player.Data.Direction * block.DashSpeed;
                    state.LastSpeed = block.DashSpeed;
                }

                if (block.AllowRedirects && player.Motor.Dash.CanDash)
                {
                    Vector2 aim = player.Input.GetAimVector(player.Data.Facing);
                    bool same = aim == player.Data.Direction;
                    if (!same || block.AllowRedirectsInSameDir)
                    {
                        player.Data.Direction = aim;
                        player.Speed = aim *
                                       ((ISourceVectorEnvironment)SourceEngine).VectorArithmetic.Length(player.Speed);
                        player.Data.Dashes = Math.Max(0, player.Data.Dashes - 1);
                        ((IOriginalGameplayProcess)SourceEngine).AudioSession.Play(
                            "event:/char/madeline/dreamblock_enter");
                        if (same)
                        {
                            player.Speed *= block.SameDirectionSpeedMultiplier;
                            player.Data.Direction *= Math.Sign(block.SameDirectionSpeedMultiplier);
                        }

                        if (player.Speed.X != 0f) player.Data.Facing = Math.Sign(player.Speed.X);
                        player.Input.Dash.ConsumeBuffer();
                        player.Input.Dash.ConsumePress();
                        player.Input.CrouchDash.ConsumeBuffer();
                        player.Input.CrouchDash.ConsumePress();
                    }
                }
            }

            return original(player);
        }

        private void EndDreamDash(Action<RoomPlayerSession> original, RoomPlayerSession player)
        {
            original(player);
            if (ReferenceEquals(player.EngineContext, SourceEngine))
                dreamSpeeds.GetValue(player, static _ => new DreamSpeedState()).LastSpeed = 0f;
        }
    }
}
