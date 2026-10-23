// SPDX-License-Identifier: MIT

using System.Linq;
using System.Numerics;
using Celeste.Core.Randomness;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Modules.Runtime;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Entities.Birds.FlutterBird;
using Celeste.Graphics.Animation;
using Celeste.Mods.FrostHelper.Modules;
using Celeste.Scene.Scheduling.ExecutionTags;

namespace Celeste.Mods.FrostHelper.Entities.Birds
{
    [CustomEntity("FrostHelper/CustomFlutterBird")]
    public sealed class CustomFlutterBird : FlutterBirdState
    {
        public bool DontFlyAway;
        public string FlyAwaySfx;
        public string HopSfx;
        private readonly ISourceEngineContext engine;

        public CustomFlutterBird(EntityData data, Vector2 offset, ISourceEngineContext engine)
            : base(data, offset, engine, CreateSprite,
                data.Attr("tweetingSfx", "event:/game/general/birdbaby_tweet_loop"))
        {
            this.engine = engine;
            var module = GetModule(engine);
            var colors = module.GetFlutterBirdColors(data.Attr("colors", "89fbff,f0fc6c,f493ff,93baff"));
            BirdSprite.Color = colors[((ISourceRandomEnvironment)engine).Random.Next(colors.Length)];
            DontFlyAway = data.Bool("dontFlyAway");
            FlyAwaySfx = data.Attr("flyAwaySfx", "event:/game/general/birdbaby_flyaway");
            HopSfx = data.Attr("hopSfx", "event:/game/general/birdbaby_hop");
        }

        private static FrostHelperModule GetModule(ISourceEngineContext engine) =>
            ((ISourceEverestModulesEnvironment)engine).Modules.OfType<FrostHelperModule>().Single();

        private static Sprite CreateSprite(ISourceEngineContext engine, EntityData data)
        {
            string directory = data.Attr("directory", "scenery/flutterbird/");
            if (!directory.EndsWith("/"))
            {
                directory += "/";
                data.Values["directory"] = directory;
            }

            return GetModule(engine).CreateFlutterBirdSprite(directory);
        }

        protected override bool CanFlyAway => !DontFlyAway;
        protected override string HopEvent => HopSfx;
        protected override string FlyAwayEvent => FlyAwaySfx;

        protected override void OnFlyAway() => AddTag(((ISourcePersistentTransitionExecutionTagsEnvironment)engine)
            .PersistentTransitionExecutionTags.Persistent.Value);
    }
}
