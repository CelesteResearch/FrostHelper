#nullable enable

using System;
using System.Linq;
using System.Threading;
using Celeste.Core.Scheduling;
using Celeste.Core.Reflection.Interop;
using Celeste.Everest.Modules.Runtime;
using Celeste.Mods.FrostHelper.Components.Lifecycle;
using Celeste.Scene.Scheduling;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Triggers.Foundation;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.FrostHelper.Runtime.Activators
{
    public sealed class FrostActivatorServices : IDisposable
    {
        private readonly ISourceEngineContext engine;
        private Hook? postAwakeHook;
        private IDisposable? exportLease;
        private Action<SourceTriggerEntityState, SourceTriggerEntityState, RoomPlayerSession?>? activated;

        public FrostActivatorServices(ISourceEngineContext engine) => this.engine = engine;

        public void Load()
        {
            postAwakeHook = new Hook(
                typeof(SceneUpdateScheduler).GetMethod(nameof(SceneUpdateScheduler.CommitQueuedEntityLists)),
                (UpdateListsHook)AfterUpdateLists);
            try
            {
                exportLease = ((ISourceModInteropEnvironment)engine).ModInterop.RegisterOwned(
                    new SourceModInteropActualTypeBinding(typeof(FrostActivatorEvents),
                        new FrostActivatorEvents(this)));
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private delegate void UpdateListsOriginal(SceneUpdateScheduler scheduler);

        private delegate void UpdateListsHook(UpdateListsOriginal original, SceneUpdateScheduler scheduler);

        private void AfterUpdateLists(UpdateListsOriginal original, SceneUpdateScheduler scheduler)
        {
            original(scheduler);
            if (scheduler.ActualSceneIdentity is not ISourceEngineContextScene scene ||
                !ReferenceEquals(scene.EngineContext, engine)) return;
            foreach (var component in scheduler.Entities.GetComponentsTrackIfNeeded<PostAwakeHook>())
                ((PostAwakeHook)component).InvokeAfterAwake();
        }

        public IDisposable Subscribe(string modName,
            Action<SourceTriggerEntityState, SourceTriggerEntityState, RoomPlayerSession?> callback)
        {
            if (modName == null) throw new ArgumentNullException(nameof(modName));
            if (!((ISourceEverestModulesEnvironment)engine).Modules.Any(module => module.Metadata?.Name == modName))
                throw new Exception(
                    $"Attempt to subscribe to event 'OnActivatorActivate' from a non-existing mod '{modName}'.");
            Action<SourceTriggerEntityState, SourceTriggerEntityState, RoomPlayerSession?>? old, next;
            do
            {
                old = Volatile.Read(ref activated);
                next =
                    (Action<SourceTriggerEntityState, SourceTriggerEntityState, RoomPlayerSession?>?)Delegate.Combine(
                        old, callback);
            } while (!ReferenceEquals(Interlocked.CompareExchange(ref activated, next, old), old));

            return new Subscription(this, callback);
        }

        internal void InvokeActivated(SourceTriggerEntityState activator, SourceTriggerEntityState target,
            RoomPlayerSession? player)
            => Volatile.Read(ref activated)?.Invoke(activator, target, player);

        private void Unsubscribe(
            Action<SourceTriggerEntityState, SourceTriggerEntityState, RoomPlayerSession?> callback)
        {
            Action<SourceTriggerEntityState, SourceTriggerEntityState, RoomPlayerSession?>? old, next;
            do
            {
                old = Volatile.Read(ref activated);
                next = (Action<SourceTriggerEntityState, SourceTriggerEntityState, RoomPlayerSession?>?)Delegate.Remove(
                    old, callback);
            } while (!ReferenceEquals(Interlocked.CompareExchange(ref activated, next, old), old));
        }

        public void Dispose()
        {
            exportLease?.Dispose();
            exportLease = null;
            postAwakeHook?.Dispose();
            postAwakeHook = null;
            activated = null;
        }

        private sealed class Subscription : IDisposable
        {
            private FrostActivatorServices? owner;
            private readonly Action<SourceTriggerEntityState, SourceTriggerEntityState, RoomPlayerSession?> callback;

            public Subscription(FrostActivatorServices owner,
                Action<SourceTriggerEntityState, SourceTriggerEntityState, RoomPlayerSession?> callback)
            {
                this.owner = owner;
                this.callback = callback;
            }

            public void Dispose() => Interlocked.Exchange(ref owner, null)?.Unsubscribe(callback);
        }
    }
}
