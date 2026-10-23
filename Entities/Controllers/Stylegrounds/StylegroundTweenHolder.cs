using System.Runtime.CompilerServices;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Scheduling;
using Celeste.Core.Reflection.Runtime;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Mods.FrostHelper.Components.Lifecycle;

namespace Celeste.Mods.FrostHelper.Entities.Controllers.Stylegrounds
{
    [RuntimeTypeAlias("FrostHelper.StylegroundMoveTrigger+TweenHolder")]
    public sealed class StylegroundTweenHolder : SceneEntityState
    {
        private sealed class Pending
        {
            internal StylegroundTweenHolder Value;
        }

        private static readonly ConditionalWeakTable<object, Pending> pendingControllers = new();

        public StylegroundTweenHolder(ISourceEngineContext engine)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, Vector2.Zero)
        {
        }

        public static StylegroundTweenHolder Ensure(ISourceEngineContext engine, object scene)
        {
            var scheduler = ((ISourceScene)scene).SourceScene;
            if (scheduler.Entities.TryGetTrackedEntities(typeof(StylegroundTweenHolder), out var tracked) &&
                tracked.Count != 0) return (StylegroundTweenHolder)tracked[0];
            var pending = pendingControllers.GetValue(scene, _ => new Pending());
            if (pending.Value == null)
            {
                pending.Value = new StylegroundTweenHolder(engine);
                scheduler.QueueSourceEntity(pending.Value);
                pending.Value.OwnedComponents.Add(new OnAwakeCallbackComponent(() => pending.Value = null));
            }

            return pending.Value;
        }
    }
}
