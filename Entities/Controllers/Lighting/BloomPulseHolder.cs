using System.Numerics;
using System.Runtime.CompilerServices;
using Celeste.Core.Components;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Mods.FrostHelper.Components.Lifecycle;
using Celeste.Scene.Entities;
using Celeste.Scene.Tagging;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;

namespace Celeste.Mods.FrostHelper.Entities.Controllers.Lighting
{
    [SceneTracked(false)]
    [RuntimeTypeAlias("FrostHelper.BloomColorPulseTrigger+PulseHolder")]
    public sealed class BloomPulseHolder : SceneEntityState
    {
        private sealed class Pending
        {
            public BloomPulseHolder Value;
        }

        private static readonly ConditionalWeakTable<object, Pending> PendingHolders = new();

        public BloomPulseHolder(ISourceEngineContext engine)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, Vector2.Zero)
        {
            Tags = ((ISourceBitTagEnvironment)engine).BitTags.Get("Global").Value;
            Active = true;
            Visible = false;
        }

        public static BloomPulseHolder Ensure(ISourceEngineContext engine, object scene)
        {
            var scheduler = ((ISourceScene)scene).SourceScene;
            if (scheduler.Entities.TryGetTrackedEntities(typeof(BloomPulseHolder), out var entities) &&
                entities.Count != 0)
                return (BloomPulseHolder)entities[0];
            var pending = PendingHolders.GetValue(scene, _ => new Pending());
            if (pending.Value == null)
            {
                pending.Value = new BloomPulseHolder(engine);
                scheduler.AddEntity(pending.Value);
                pending.Value.Add(new OnAwakeCallbackComponent(() => pending.Value = null));
            }

            return pending.Value;
        }
    }
}
