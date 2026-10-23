// SPDX-License-Identifier: MIT

using System;
using Celeste.Mods.FrostHelper.Entities.Boosters;
using Celeste.Scene.Entities;
using Celeste.Simulation;
using MonoMod.ModInterop;

namespace Celeste.Mods.FrostHelper.Runtime.Boosters
{
    [ModExportName("FrostHelper")]
    public sealed class FrostBoosterExports
    {
        private readonly FrostBoosterRuntime runtime;

        public FrostBoosterExports(FrostBoosterRuntime runtime)
        {
            this.runtime = runtime;
        }

        public void SetCustomBoostState(RoomPlayerSession player, GenericCustomBooster booster) =>
            runtime.ForPlayer(player).SetCustomBoostState(booster);

        public bool IsInCustomBoostState(RoomPlayerSession player) =>
            runtime.TryGetPlayer(player, out var state) && state.IsInBoostState;

        public int GetCustomBoostStateId() => runtime.LastBoostState;
        public int GetCustomRedBoostStateId() => runtime.LastRedBoostState;

        public void SubscribeToCustomBoostBegin(Action<RoomPlayerSession, SceneEntityState> onBegin) =>
            runtime.BoostBegan += onBegin;

        public void UnsubscribeToCustomBoostBegin(Action<RoomPlayerSession, SceneEntityState> onBegin) =>
            runtime.BoostBegan -= onBegin;
    }
}
