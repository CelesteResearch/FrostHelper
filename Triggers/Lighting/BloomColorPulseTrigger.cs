using System;
using System.Linq;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Components.Animation;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Easing;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Modules.Runtime;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Mods.FrostHelper.Animation.Easing;
using Celeste.Mods.FrostHelper.Entities.Controllers.Lighting;
using Celeste.Mods.FrostHelper.Runtime.Lighting;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Triggers.Foundation;

namespace Celeste.Mods.FrostHelper.Triggers.Lighting
{
    [CustomEntity("FrostHelper/BloomColorPulseTrigger")]
    [RuntimeTypeAlias("FrostHelper.BloomColorPulseTrigger")]
    public class BloomColorPulseTrigger : SourceTriggerEntityState
    {
        private readonly ISourceEngineContext engine;
        private readonly FrostBloomColors colors;
        public PackedColor From;
        public PackedColor To;
        public float Duration;
        public Func<float, float> Easer;
        public SourceTweenState.TweenMode TweenMode;

        public BloomColorPulseTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : this(engine, data, offset,
                ((ISourceEverestModulesEnvironment)engine).Modules.OfType<IFrostBloomEnvironment>().Single()
                .BloomColors)
        {
        }

        public BloomColorPulseTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset,
            FrostBloomColors colors)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset,
                data.Width, data.Height, ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            this.engine = engine;
            this.colors = colors;
            From = colors.Colors.Read(data.Attr("bloomAddFrom", "ffffff"));
            To = colors.Colors.Read(data.Attr("bloomAddTo", "ff00ff"));
            Duration = data.Float("duration", .4f);
            Easer = new FrostEasingResolver(((ISourceEasingEnvironment)engine).EasingArithmetic).Resolve(
                data.Attr("easing", "Linear"));
            TweenMode = data.Attr("tweenMode", "") switch
            {
                "Persist" => SourceTweenState.TweenMode.Persist,
                "Oneshot" => SourceTweenState.TweenMode.Oneshot,
                "Looping" => SourceTweenState.TweenMode.Looping,
                "YoyoOneshot" => SourceTweenState.TweenMode.YoyoOneshot,
                "YoyoLooping" => SourceTweenState.TweenMode.YoyoLooping,
                _ => SourceTweenState.TweenMode.YoyoOneshot
            };
        }

        public override void OnEnter(RoomPlayerSession player)
        {
            base.OnEnter(player);
            var tween = SourceTweenState.Create(engine.TweenPool, TweenMode, Easer, Duration, false);
            tween.OnUpdate = t => colors.Color = PackedColor.Lerp(From, To, ScalarMath.Clamp(t.Eased, 0f, 1f));
            tween.Start();
            BloomPulseHolder.Ensure(engine, Scene).Add(tween);
        }
    }
}
