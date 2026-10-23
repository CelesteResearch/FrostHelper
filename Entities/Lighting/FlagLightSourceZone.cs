using System.Linq;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Randomness;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Modules.Runtime;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.FrostHelper.Modules;
using Celeste.Scene.Entities;
using Celeste.Scene.Scheduling.ExecutionTags;
using Celeste.Simulation.Presentation.Lighting;
using Celeste.Simulation.Presentation.PostProcessing.Bloom;

namespace Celeste.Mods.FrostHelper.Entities.Lighting
{
    [CustomEntity("FrostHelper/HackfixFlagLightSourceZone")]
    [RuntimeTypeAlias("FrostHelper.Entities.Hackfixes.FlagLightSourceZone")]
    internal class FlagLightSourceZone : SceneEntityState
    {
        private static readonly PackedColor[] defaultColors = { PackedColor.White };
        public readonly string Flag;
        public readonly bool FlagInverted;

        public FlagLightSourceZone(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
            int amount = data.Int("amount", 5);
            var alpha = RangeFloat(data, "alphaMinimum", "alphaMaximum", .8f, 1f);
            var radius = RangeFloat(data, "radiusMinimum", "radiusMaximum", 46f, 48f);
            var startFade = RangeFloat(data, "startFadeMinimum", "startFadeMaximum", 22f, 24f);
            var endFade = RangeFloat(data, "endFadeMinimum", "endFadeMaximum", 46f, 48f);
            var module = ((ISourceEverestModulesEnvironment)engine).Modules.OfType<FrostHelperModule>().Single();
            var colors = module.Colors.ReadArray(data.Attr("colors", "")) ?? defaultColors;
            var random = ((ISourceRandomEnvironment)engine).Random;
            for (int index = 0; index < amount; index++)
            {
                var at = new Vector2(SourceRandomMath.Range(random, 0f, data.Width),
                    SourceRandomMath.Range(random, 0f, data.Height));
                float opacity = Range(random, alpha);
                Add(new VertexLightState(engine, at, colors[random.Next(colors.Length)], opacity,
                    (int)Range(random, startFade), (int)Range(random, endFade)));
                Add(new BloomPointState(at, opacity, Range(random, radius)));
            }

            Flag = data.Attr("flag", "");
            FlagInverted = data.Bool("flagInverted", false);
            AddTag(((ISourcePersistentTransitionExecutionTagsEnvironment)engine).PersistentTransitionExecutionTags
                .TransitionUpdate.Value);
        }

        public override void Update()
        {
            base.Update();
            bool visible = ((OriginalLevelSceneRoot)Scene).Session.GetFlag(Flag) != FlagInverted;
            if (Visible == visible) return;
            Visible = visible;
            foreach (var component in OwnedComponents) component.Visible = visible;
        }

        private static float Range(ISourceRandom random, Vector2 range) =>
            SourceRandomMath.Range(random, range.X, range.Y);

        private static Vector2 RangeFloat(EntityData data, string minimum, string maximum, float minDefault,
            float maxDefault)
            => new(data.Float(minimum, minDefault), data.Float(maximum, maxDefault));
    }
}
