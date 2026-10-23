using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Celeste.Core.Scheduling;
using Celeste.Everest.Content.Assets;
using Celeste.Everest.Content.Registry;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Content.VirtualAssets;
using Celeste.Graphics.Effects.Composition;
using Celeste.Graphics.Effects.Packing;
using Celeste.Graphics.Rendering.Devices;
using Celeste.Graphics.Rendering.Drawing;

namespace Celeste.Mods.FrostHelper.Runtime.ZipMovers
{
    public sealed class CustomZipSpriteSource
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, SourceCache> Caches = new();
        private readonly List<AtlasEntry> innerCogs;
        private readonly AtlasEntry[,] blockTextures;
        private readonly AtlasEntry cog;
        private readonly AtlasEntry pixel;
        private readonly PackedTextureResource resource;

        public string Directory { get; }
        public string LightPath { get; }

        private CustomZipSpriteSource(ISourceEngineContext engine, string directory)
        {
            Directory = directory;
            LightPath = directory + "/light";
            var atlas = ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas;
            var groups = new List<List<AtlasEntry>>
            {
                atlas.GetAtlasSubtextures(directory + "/innercog"),
                new() { atlas[directory + "/block"], atlas[directory + "/cog"], ((ISourceDrawScope)engine).Draw.Pixel }
            };
            var packed = PackedTextureGroups.CreateGroups(((ISourceVirtualContentEnvironment)engine).VirtualContent,
                ((ISourceGraphicsDeviceEnvironment)engine).GraphicsDevice, ((ISourceDrawScope)engine).Draw.SpriteBatch,
                groups, "fh.autopacked.customZipMover." + directory, out resource);
            innerCogs = packed[0];
            blockTextures = new AtlasEntry[3, 3];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++) blockTextures[i, j] = packed[1][0].GetSubtexture(i * 8, j * 8, 8, 8);
            }

            cog = packed[1][1];
            pixel = packed[1][2];
        }

        public static CustomZipSpriteSource Get(ISourceEngineContext engine, string directory)
            => Caches.GetValue(engine, value => new SourceCache(value)).Get(directory);

        public static FullscreenRenderTargetPool PathTargets(ISourceEngineContext engine)
            => Caches.GetValue(engine, value => new SourceCache(value)).PathTargets;

        public static void Release(ISourceEngineContext engine)
        {
            if (!Caches.TryGetValue(engine, out var cache)) return;
            Caches.Remove(engine);
            cache.Dispose();
        }

        public List<AtlasEntry> InnerCogs() => innerCogs;
        public AtlasEntry[,] BlockNineSlice() => blockTextures;
        public AtlasEntry Cog() => cog;
        public AtlasEntry Pixel() => pixel;

        private sealed class SourceCache : IDisposable
        {
            private const string GameplayPrefix = "Graphics/Atlases/Gameplay/";
            private readonly ISourceEngineContext engine;
            private readonly EverestContentRegistry registry;

            private readonly Dictionary<string, CustomZipSpriteSource> entries = new();

            // Existing entities retain their old source when an atlas directory changes.
            private readonly List<CustomZipSpriteSource> retainedSources = new();
            private readonly List<string> invalidatedKeys = new();
            public FullscreenRenderTargetPool PathTargets { get; }

            public SourceCache(ISourceEngineContext engine)
            {
                this.engine = engine;
                registry = ((IEverestContentRegistryEnvironment)engine).SourceContentRegistry;
                PathTargets = new FullscreenRenderTargetPool(((ISourceVirtualContentEnvironment)engine).VirtualContent,
                    ((ISourceGraphicsDeviceEnvironment)engine).GraphicsDevice);
                registry.OnUpdate += OnContentChanged;
            }

            public CustomZipSpriteSource Get(string directory)
            {
                if (entries.TryGetValue(directory, out var result)) return result;
                result = new CustomZipSpriteSource(engine, directory);
                entries.Add(directory, result);
                retainedSources.Add(result);
                return result;
            }

            private void OnContentChanged(EverestContentAsset from, EverestContentAsset to)
            {
                string path = from?.PathVirtual;
                if (path == null || !path.StartsWith(GameplayPrefix, StringComparison.Ordinal)) return;
                string spritePath = path.Substring(GameplayPrefix.Length);
                foreach (string key in entries.Keys)
                {
                    if (spritePath.StartsWith(key, StringComparison.Ordinal)) invalidatedKeys.Add(key);
                }

                foreach (string key in invalidatedKeys) entries.Remove(key);
                invalidatedKeys.Clear();
            }

            public void Dispose()
            {
                registry.OnUpdate -= OnContentChanged;
                foreach (var source in retainedSources) source.resource?.Dispose();
                PathTargets.Dispose();
                retainedSources.Clear();
                entries.Clear();
            }
        }
    }
}
