using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Celeste.Core.Scheduling;
using Celeste.Formats.Graphics.Sprites;
using Celeste.Graphics.Animation;

namespace Celeste.Mods.FrostHelper.Runtime.Rendering
{
    public static class FrostCustomSpriteFactory
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, Dictionary<string, SpriteData>>
            Cache = new();

        public static Sprite Create(ISourceEngineContext engine, SpriteBank bank, string id, string directory)
        {
            var cache = Cache.GetOrCreateValue(engine);
            if (!cache.TryGetValue(directory, out var data))
            {
                var original = bank.SpriteData[id];
                data = new SpriteData(engine, original.Atlas);
                foreach (var source in original.Sources) data.Add(source.XML, directory);
                cache.Add(directory, data);
            }

            return data.Sprite.CreateClone();
        }
    }
}
