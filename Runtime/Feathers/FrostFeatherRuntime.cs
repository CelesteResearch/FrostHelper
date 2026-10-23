using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Reflection;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Runtime;
using Celeste.Mods.FrostHelper.Modules;
using Celeste.Mods.FrostHelper.Runtime.Boosters;
using Celeste.Simulation;

namespace Celeste.Mods.FrostHelper.Runtime.Feathers
{
    public sealed class FrostFeatherRuntime : IDisposable
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, FrostFeatherRuntime> Owners = new();
        private readonly ConditionalWeakTable<RoomPlayerSession, FrostFeatherPlayerState> players = new();

        private readonly ConditionalWeakTable<RoomPlayerSession, FrostFeatherPlayerState>.CreateValueCallback
            createPlayer;

        private static readonly Func<FrostHelperModule, FrostBoosterRuntime> GetBoosters =
            (Func<FrostHelperModule, FrostBoosterRuntime>)typeof(FrostHelperModule).GetProperty("Boosters",
                    BindingFlags.Instance | BindingFlags.NonPublic).GetMethod
                .CreateDelegate(typeof(Func<FrostHelperModule, FrostBoosterRuntime>));

        private FrostFeatherHooks hooks;
        private FrostBoosterRuntime boosters;
        internal FrostFeatherMethods Methods { get; }
        public ISourceEngineContext Engine { get; }

        private FrostFeatherRuntime(ISourceEngineContext engine)
        {
            Engine = engine;
            createPlayer = CreatePlayer;
            Methods = new FrostFeatherMethods();
        }

        public static FrostFeatherRuntime For(ISourceEngineContext engine) =>
            Owners.GetValue(engine, static key => new FrostFeatherRuntime(key));

        public void Load()
        {
            if (hooks != null) return;
            boosters = GetBoosters(((ISourceEverestModulesEnvironment)Engine).Modules.OfType<FrostHelperModule>()
                .Single());
            hooks = new FrostFeatherHooks(this);
        }

        private FrostFeatherPlayerState CreatePlayer(RoomPlayerSession player)
        {
            boosters.ForPlayer(player);
            return new FrostFeatherPlayerState(this, player);
        }

        public FrostFeatherPlayerState ForPlayer(RoomPlayerSession player) => players.GetValue(player, createPlayer);

        internal bool TryGetPlayer(RoomPlayerSession player, out FrostFeatherPlayerState state) =>
            players.TryGetValue(player, out state);

        internal int Replacement(int original, RoomPlayerSession player)
        {
            if (player == null || !ReferenceEquals(player.EngineContext, Engine)) return original;
            if (original == 19 && players.TryGetValue(player, out var feather) &&
                player.Machine.State == feather.StateId) return feather.StateId;
            if (original == 5 && boosters.TryGetPlayer(player, out var booster) &&
                player.Machine.State == booster.RedState) return booster.RedState;
            return original;
        }

        public static void Unload(ISourceEngineContext engine)
        {
            if (Owners.TryGetValue(engine, out var owner)) owner.Dispose();
            Owners.Remove(engine);
        }

        public void Dispose()
        {
            hooks?.Dispose();
            hooks = null;
            Methods.Dispose();
            if (Owners.TryGetValue(Engine, out var current) && ReferenceEquals(current, this)) Owners.Remove(Engine);
        }
    }
}
