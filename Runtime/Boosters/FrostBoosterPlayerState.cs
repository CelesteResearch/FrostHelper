// SPDX-License-Identifier: MIT

using System;
using System.Collections;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;
using Celeste.Core.Scheduling;
using Celeste.Mods.FrostHelper.Entities.Boosters;
using Celeste.Physics.Collision;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Dash;
using Celeste.Simulation.Gameplay.Platforms.Foundation;

namespace Celeste.Mods.FrostHelper.Runtime.Boosters
{
    public sealed class FrostBoosterPlayerState
    {
        private readonly FrostBoosterRuntime runtime;
        private readonly RoomPlayerSession player;
        private readonly PlayerBoostSequence sequence;
        private readonly Func<bool> canDashOut;
        private readonly Action beforeDash;
        private readonly Action correctJumpThrus;
        private Vector2 boostTarget;
        private bool boostRed;
        public GenericCustomBooster Booster;
        public int BoostState { get; }
        public int RedState { get; }
        public bool IsInBoostState => player.Machine.State == BoostState;
        private static readonly Func<RoomPlayerSession, PlayerBoostSequence> GetSequence = Getter();

        public FrostBoosterPlayerState(FrostBoosterRuntime runtime, RoomPlayerSession player)
        {
            this.runtime = runtime;
            this.player = player;
            sequence = GetSequence(player);
            canDashOut = CanDashOut;
            beforeDash = BeforeDash;
            correctJumpThrus = CorrectJumpThrus;
            var machine = (ISourceNamedStateMachine)player.Machine;
            BoostState = machine.AddState("Custom Boost", Update, Coroutine, Begin, End);
            RedState = machine.AddState("Custom Red Boost", RedUpdate, RedCoroutine, sequence.RedDashBegin);
        }

        public void Boost(GenericCustomBooster booster)
        {
            SetCustomBoostState(booster);
            booster.EnterSpeed = player.Speed;
            player.Speed = Vector2.Zero;
            boostTarget = booster.Center;
            boostRed = booster.Red;
            booster.StartedBoosting = true;
        }

        public void SetCustomBoostState(GenericCustomBooster booster)
        {
            Booster = booster;
            player.Machine.State = BoostState;
        }

        private void Begin()
        {
            Booster?.HandleBoostBegin(player);
            runtime.OnBoostBegin(player, Booster);
        }

        private void MoveTo(Vector2 target)
        {
            runtime.Gravity.BeginOverride?.Invoke();
            player.MoveToX(target.X);
            player.MoveToY(target.Y);
            runtime.Gravity.EndOverride?.Invoke();
        }

        private int Update()
        {
            var booster = Booster;
            MoveTo(PlayerBoostMotion.ApproachPosition(runtime.Engine, player.Body.ExactPosition, boostTarget,
                player.Collider.Center, player.Input.Aim.Value * 3f));
            if (booster != null && (player.Input.Dash.Pressed || player.Input.CrouchDashPressed) &&
                booster.CanFastbubble())
            {
                player.Data.DemoDashed = player.Input.CrouchDashPressed;
                player.Input.Dash.ConsumePress();
                player.Input.CrouchDash.ConsumePress();
                return ExitBoostState(booster);
            }

            return BoostState;
        }

        private void End()
        {
            MoveTo(PlayerBoostMotion.ReleasePosition(boostTarget, player.Collider.Center));
            Booster?.OnBoostEnd(player);
        }

        private IEnumerator Coroutine() => Booster.HandleBoostCoroutine(player);

        public int ExitBoostState(GenericCustomBooster booster)
        {
            float speed = booster.GetBoostSpeed();
            runtime.Session.NextDashSpeed = speed;
            if (boostRed) return RedState;
            runtime.Session.NextSuperJumpSpeed = speed + 20f;
            return 2;
        }

        private bool CanDashOut()
        {
            int old = player.Data.Dashes;
            player.Data.Dashes = Booster.RedBoostDashOutMode switch
            {
                RedBoostDashOutModes.Default => old, RedBoostDashOutModes.EvenAtZeroDashes => 1,
                RedBoostDashOutModes.Never => 0, _ => throw new InvalidOperationException()
            };
            bool result = player.Motor.Dash.CanDash;
            player.Data.Dashes = old;
            return result;
        }

        private void BeforeDash() => runtime.CancelFirstBoosting(player);

        private int RedUpdate() => PlayerRedBoostMotion.Update(player, player.Input, player.World, player.Motor.Dash,
            RedState, false, canDashOut, beforeDash, correctJumpThrus);

        private IEnumerator RedCoroutine() => sequence.RedDashCoroutine();

        private void CorrectJumpThrus()
        {
            bool inverted = runtime.Gravity.IsPlayerInverted?.Invoke() ?? false;
            foreach (SourceJumpThruEntityState platform in ((ISourceScene)player.Scene).SourceScene.Entities
                     .GetTrackedEntities("Celeste.JumpThru"))
            {
                float offset = inverted ? platform.Bottom - player.Top : player.Bottom - platform.Top;
                if (SourceEntityCollision.Check(player, platform) && offset <= 6f) player.MoveVExact((int)-offset);
            }
        }

        private static Func<RoomPlayerSession, PlayerBoostSequence> Getter()
        {
            var method = new DynamicMethod("boostSequence", typeof(PlayerBoostSequence),
                new[] { typeof(RoomPlayerSession) }, typeof(RoomPlayerSession), true);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld,
                typeof(RoomPlayerSession).GetField("boostSequence", BindingFlags.Instance | BindingFlags.NonPublic));
            il.Emit(OpCodes.Ret);
            return (Func<RoomPlayerSession, PlayerBoostSequence>)method.CreateDelegate(
                typeof(Func<RoomPlayerSession, PlayerBoostSequence>));
        }
    }
}
