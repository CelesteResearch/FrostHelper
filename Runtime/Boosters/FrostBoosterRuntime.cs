// SPDX-License-Identifier: MIT

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Numerics;
using Celeste.Core.Reflection.Interop;
using Celeste.Core.Scheduling;
using Celeste.Mods.FrostHelper.Color;
using Celeste.Mods.FrostHelper.Entities.Boosters;
using Celeste.Mods.FrostHelper.Sessions;
using Celeste.Scene.Tracking;
using Celeste.Scene.Entities;
using Celeste.Simulation;

namespace Celeste.Mods.FrostHelper.Runtime.Boosters
{
    public sealed class FrostBoosterRuntime : IDisposable
    {
        private readonly ConditionalWeakTable<RoomPlayerSession, FrostBoosterPlayerState> players = new();
        private readonly Func<FrostHelperSession> session;
        private readonly IDisposable importLease;
        private readonly IDisposable exportLease;
        private readonly SourceModInteropProcess interop;
        private Action<string> registeredBlacklist;

        private readonly ConditionalWeakTable<RoomPlayerSession, FrostBoosterPlayerState>.CreateValueCallback
            createPlayer;

        public event Action<RoomPlayerSession, SceneEntityState> BoostBegan;
        public ISourceEngineContext Engine { get; }
        public ColorReader Colors { get; }
        public Action<string> Warn { get; }
        public BoosterGravityImports Gravity { get; } = new();
        public int LastBoostState { get; private set; } = int.MaxValue;
        public int LastRedBoostState { get; private set; } = int.MaxValue;
        public FrostHelperSession Session => session();
        public Vector2 PlayerOffset => new(0, Gravity.IsPlayerInverted?.Invoke() == true ? 2f : -2f);

        public FrostBoosterRuntime(ISourceEngineContext engine, ColorReader colors, Action<string> warn,
            Func<FrostHelperSession> session)
        {
            Engine = engine;
            Colors = colors;
            Warn = warn;
            this.session = session;
            createPlayer = p =>
            {
                var state = new FrostBoosterPlayerState(this, p);
                LastBoostState = state.BoostState;
                LastRedBoostState = state.RedState;
                return state;
            };
            interop = ((ISourceModInteropEnvironment)engine).ModInterop;
            importLease = interop.RegisterOwned(new SourceModInteropActualTypeBinding(typeof(BoosterGravityImports),
                Gravity, exportedMethods: Array.Empty<MethodInfo>()));
            try
            {
                exportLease = interop.RegisterOwned(new SourceModInteropActualTypeBinding(typeof(FrostBoosterExports),
                    new FrostBoosterExports(this)));
            }
            catch
            {
                importLease.Dispose();
                throw;
            }

            interop.ImportsRefreshed += RefreshBlacklist;
            RefreshBlacklist();
        }

        public FrostBoosterPlayerState ForPlayer(RoomPlayerSession player) => players.GetValue(player, createPlayer);

        internal void OnBoostBegin(RoomPlayerSession player, GenericCustomBooster booster) =>
            BoostBegan?.Invoke(player, booster);

        public bool TryGetPlayer(RoomPlayerSession player, out FrostBoosterPlayerState state) =>
            players.TryGetValue(player, out state);

        public void CancelFirstBoosting(RoomPlayerSession player)
        {
            foreach (GenericCustomBooster booster in ((ISourceScene)player.Scene).SourceScene.Entities
                     .GetTrackedEntities(
                         typeof(GenericCustomBooster)))
                if (booster.BoostingPlayer)
                {
                    booster.OnPlayerDashed(Vector2.Zero);
                    break;
                }
        }

        public bool ShouldCallDashEvents(RoomPlayerSession player)
        {
            if (players.TryGetValue(player, out var state) && state.Booster is { BoostingPlayer: true })
            {
                player.Data.CalledEvents = false;
                state.Booster = null;
                return false;
            }

            foreach (GenericCustomBooster booster in ((ISourceScene)player.Scene).SourceScene.Entities
                     .GetTrackedEntities(
                         typeof(GenericCustomBooster)))
            {
                if (booster.StartedBoosting)
                {
                    booster.PlayerBoosted(player, player.Data.Direction);
                    return false;
                }

                if (booster.BoostingPlayer && state?.Booster == booster) return false;
            }

            return true;
        }

        public float ConsumeDashSpeed(float original)
        {
            var next = Session.NextDashSpeed;
            Session.NextDashSpeed = null;
            return next ?? original;
        }

        public float ConsumeSuperJumpSpeed(float original)
        {
            var next = Session.NextSuperJumpSpeed;
            Session.NextSuperJumpSpeed = null;
            return next ?? original;
        }

        public void ResetSpeed()
        {
            Session.NextDashSpeed = null;
            Session.NextSuperJumpSpeed = null;
        }

        private void RefreshBlacklist()
        {
            var action = Gravity.RegisterModSupportBlacklist;
            if (action != null && action != registeredBlacklist) action("FrostHelper");
            registeredBlacklist = action;
        }

        public void Dispose()
        {
            interop.ImportsRefreshed -= RefreshBlacklist;
            exportLease.Dispose();
            importLease.Dispose();
        }
    }
}
