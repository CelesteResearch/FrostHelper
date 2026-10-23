// SPDX-License-Identifier: MIT

using Celeste.Core.Scheduling;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Celeste.Everest.Entities.Triggers.Lava;
using Celeste.Mods.FrostHelper.Entities.Lava;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.FrostHelper.Runtime.Lava
{
    public sealed class RisingLavaBlockerHooks : IDisposable
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, RisingLavaBlockerHooks> owners = new();
        private static readonly Func<EverestLavaBlockerTriggerState, bool> isEnabled = CreateEnabledGetter();
        private readonly ISourceEngineContext engine;
        private Hook stay;
        private Hook leave;

        private RisingLavaBlockerHooks(ISourceEngineContext engine)
        {
            this.engine = engine;
        }

        private static Func<EverestLavaBlockerTriggerState, bool> CreateEnabledGetter()
        {
            var trigger = Expression.Parameter(typeof(EverestLavaBlockerTriggerState));
            return Expression
                .Lambda<Func<EverestLavaBlockerTriggerState, bool>>(Expression.Field(trigger, "enabled"), trigger)
                .Compile();
        }

        public static RisingLavaBlockerHooks For(ISourceEngineContext engine)
        {
            var hooks = owners.GetValue(engine, key => new(key));
            hooks.stay ??= new Hook(
                typeof(EverestLavaBlockerTriggerState).GetMethod(nameof(EverestLavaBlockerTriggerState.OnStay)),
                (Action<Action<EverestLavaBlockerTriggerState, RoomPlayerSession>, EverestLavaBlockerTriggerState,
                    RoomPlayerSession>)hooks.OnStay);
            hooks.leave ??= new Hook(
                typeof(EverestLavaBlockerTriggerState).GetMethod(nameof(EverestLavaBlockerTriggerState.OnLeave)),
                (Action<Action<EverestLavaBlockerTriggerState, RoomPlayerSession>, EverestLavaBlockerTriggerState,
                    RoomPlayerSession>)hooks.OnLeave);
            return hooks;
        }

        private void SetWaiting(EverestLavaBlockerTriggerState trigger, bool waiting)
        {
            if (trigger == null || !isEnabled(trigger)) return;
            if (trigger.Scene is not ISourceScene scene ||
                !ReferenceEquals(((ISourceEngineContextScene)trigger.Scene).EngineContext, engine)) return;
            if (!scene.SourceScene.Entities.TryGetTrackedEntities(typeof(CustomRisingLava), out List<object> lavas))
                return;
            foreach (CustomRisingLava lava in lavas) lava.waiting = waiting;
        }

        private void OnStay(Action<EverestLavaBlockerTriggerState, RoomPlayerSession> original,
            EverestLavaBlockerTriggerState trigger, RoomPlayerSession player)
        {
            SetWaiting(trigger, true);
            original(trigger, player);
        }

        private void OnLeave(Action<EverestLavaBlockerTriggerState, RoomPlayerSession> original,
            EverestLavaBlockerTriggerState trigger, RoomPlayerSession player)
        {
            SetWaiting(trigger, false);
            original(trigger, player);
        }

        public static void Unload(ISourceEngineContext engine)
        {
            if (owners.TryGetValue(engine, out var hooks)) hooks.Dispose();
        }

        public void Dispose()
        {
            stay?.Dispose();
            stay = null;
            leave?.Dispose();
            leave = null;
        }
    }
}
