// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Celeste.Core.Randomness;
using Celeste.Core.Scheduling;
using Celeste.Mods.FrostHelper.Entities.Bubbles;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Movement;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.FrostHelper.Runtime.Bubbles
{
    public sealed class BubblerRuntime : IDisposable
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, BubblerRuntime> contexts = new();
        private readonly ISourceEngineContext engine;
        private Hook normalBeginHook;
        private ISourceRandom visualRandom;

        public ISourceRandom VisualRandom => visualRandom ??=
            ((ISourceRandomProcessEnvironment)engine).RandomProcess.Factory.CreateUnseeded();

        private BubblerRuntime(ISourceEngineContext engine)
        {
            this.engine = engine;
        }

        public static BubblerRuntime For(ISourceEngineContext engine)
        {
            var runtime = contexts.GetValue(engine, key => new BubblerRuntime(key));
            runtime.normalBeginHook ??= new Hook(typeof(NormalSequence).GetMethod(nameof(NormalSequence.Begin)),
                (Action<Action<NormalSequence>, NormalSequence>)runtime.AfterNormalBegin);
            return runtime;
        }

        public static void Unload(ISourceEngineContext engine)
        {
            if (contexts.TryGetValue(engine, out var runtime)) runtime.Dispose();
        }

        private void AfterNormalBegin(Action<NormalSequence> original, NormalSequence sequence)
        {
            original(sequence);
            if (!ReferenceEquals(sequence.EngineContext, engine) || sequence.World is not RoomPlayerWorld world ||
                world.SourcePlayer.Scene is not ISourceScene scene)
                return;
            if (!scene.SourceScene.Entities.TryGetTrackedEntities(typeof(Bubbler), out List<object> entities)) return;
            foreach (Bubbler bubbler in entities)
                bubbler.FinishIfFlying();
        }

        public void Dispose()
        {
            normalBeginHook?.Dispose();
            normalBeginHook = null;
        }
    }
}
