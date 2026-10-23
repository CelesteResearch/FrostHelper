using Celeste.Core.Scheduling;
using Celeste.Core.Text;
using Celeste.Everest.Content.Registry;
using Celeste.Everest;
using Celeste.Everest.Content.Types.Text;
using Celeste.Everest.Diagnostics.Logging;
using YamlDotNet.Serialization;

namespace Celeste.Mods.FrostHelper.Content.Spinners
{
    public sealed class SpinnerAnimationMetadata
    {
        public float Speed { get; set; } = 12f;
        public bool RandomizeStartFrame { get; set; } = true;
        public LoopModes Mode { get; set; } = LoopModes.Loop;
        private string frames;
        private int[] framesArray;

        public string Frames
        {
            get => frames;
            set
            {
                frames = value;
                framesArray = null;
            }
        }

        [YamlIgnore]
        public int[] FramesArray => Frames == null ? null : framesArray ??= SourceCsvIntegerFrames.Read(Frames);

        public enum LoopModes
        {
            Loop,
            Once
        }

        public static SpinnerAnimationMetadata GetForTexture(ISourceEngineContext engine, params string[] paths)
        {
            var registry = ((IEverestContentRegistryEnvironment)engine).SourceContentRegistry;
            foreach (string path in paths)
            {
                string full = "Graphics/Atlases/Gameplay/" + path + (path.EndsWith("/") ? "" : ".") + "fhAnimation";
                var yaml = registry.Get<AssetTypeYaml>(full);
                if (yaml == null) continue;
                if (yaml.TryDeserialize<SpinnerAnimationMetadata>(out var metadata)) return metadata;
                ((IEverestLoggerProcessEnvironment)engine).Logger.Log(EverestLogLevel.Warn,
                    "FrostHelper.AnimationMeta", "Failed to load animation .yaml from '" + full + "'.");
            }

            return new SpinnerAnimationMetadata();
        }
    }
}
