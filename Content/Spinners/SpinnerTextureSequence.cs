using System;
using System.Collections.Generic;
using Celeste.Core.Randomness;
using Celeste.Graphics.Content.Atlas;

namespace Celeste.Mods.FrostHelper.Content.Spinners
{
    public sealed class SpinnerTextureSequence
    {
        private readonly List<AtlasEntry> sourceTextures;
        private readonly List<AtlasEntry> textures;
        private readonly SpinnerAnimationMetadata metadata;
        private Dictionary<(int, int, int, int), SpinnerTextureSequence> subtextures;
        public AtlasEntry First { get; }
        public bool Animated => metadata != null;
        public float Speed => metadata?.Speed ?? 0f;
        public int FrameCount => textures.Count;

        public SpinnerTextureSequence(AtlasEntry texture)
        {
            First = texture;
            sourceTextures = textures = new List<AtlasEntry> { texture };
        }

        public SpinnerTextureSequence(List<AtlasEntry> sourceTextures, SpinnerAnimationMetadata metadata)
        {
            this.sourceTextures = sourceTextures;
            this.metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
            var first = sourceTextures[0];
            First = first.GetSubtexture(0, 0, first.ClipRect.Width, first.ClipRect.Height);
            if (metadata.FramesArray == null) textures = sourceTextures;
            else
            {
                textures = new List<AtlasEntry>(metadata.FramesArray.Length);
                foreach (int index in metadata.FramesArray)
                    if (index >= 0 && index < sourceTextures.Count)
                        textures.Add(sourceTextures[index]);
            }
        }

        public AtlasEntry GetFrame(float time, float offset) => GetFrame(time, offset, Speed);

        public AtlasEntry GetFrame(float time, float offset, float speed)
        {
            if (!Animated) return First;
            int index = (int)(time * speed + offset);
            index = metadata.Mode == SpinnerAnimationMetadata.LoopModes.Loop
                ? index % textures.Count
                : Math.Min(index, textures.Count - 1);
            return textures[Math.Max(index, 0)];
        }

        public float CreateRandomAnimOffset(ISourceRandom random)
            => Animated && metadata.RandomizeStartFrame ? SourceRandomMath.NextFloat(random, textures.Count) : 0f;

        public SpinnerTextureSequence GetSubtexture(int x, int y, int width, int height)
        {
            if (!Animated) return new SpinnerTextureSequence(First.GetSubtexture(x, y, width, height));
            var frames = new List<AtlasEntry>(sourceTextures.Count);
            foreach (var source in sourceTextures) frames.Add(source.GetSubtexture(x, y, width, height));
            return new SpinnerTextureSequence(frames, metadata);
        }

        public SpinnerTextureSequence GetSubtextureCached(int x, int y, int width, int height)
        {
            subtextures ??= new Dictionary<(int, int, int, int), SpinnerTextureSequence>();
            var key = (x, y, width, height);
            if (!subtextures.TryGetValue(key, out var texture))
                subtextures.Add(key, texture = GetSubtexture(x, y, width, height));
            return texture;
        }
    }
}
