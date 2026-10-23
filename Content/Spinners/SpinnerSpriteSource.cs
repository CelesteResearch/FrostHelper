using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Scheduling;
using Celeste.Everest;
using Celeste.Everest.Diagnostics.Logging;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Atlas;

namespace Celeste.Mods.FrostHelper.Content.Spinners
{
    public sealed class SpinnerSpriteSource
    {
        private readonly ISourceEngineContext engine;
        private readonly AtlasData atlas;
        private readonly List<SpinnerTextureSequence> foreground = new();
        private readonly List<SpinnerTextureSequence> background = new();
        private List<SpinnerTextureSequence> hotForeground, hotBackground, foregroundDecoration, backgroundDecoration;
        public string OrigDirectoryString { get; }
        public string Directory { get; }
        public string SpritePathSuffix { get; }
        public bool Animated { get; }
        public bool HasDeco { get; }
        public Vector2 CullingDistance { get; }
        public float ConnectionWidth { get; }

        public SpinnerSpriteSource(ISourceEngineContext engine, string directory, string suffix, bool animated = false)
        {
            this.engine = engine;
            atlas = ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas;
            OrigDirectoryString = directory;
            if (directory.EndsWith("!"))
            {
                animated = true;
                directory = directory.Substring(0, directory.Length - 1);
            }

            int separator = directory.IndexOf('>');
            if (separator >= 0)
            {
                suffix = directory.Substring(separator + 1);
                directory = directory.Substring(0, separator);
            }

            Directory = directory;
            SpritePathSuffix = suffix;
            Animated = animated;
            HasDeco = atlas.Has(BackgroundPath(false) + "Deco00");
            if (animated)
            {
                for (int i = 0;; i++)
                {
                    string next = directory + "/" + (i <= 9 ? "0" + i : i.ToString());
                    bool found = false;
                    if (atlas.Has(next + "/fg" + suffix + "00"))
                    {
                        found = true;
                        string path = next + "/fg" + suffix;
                        foreground.Add(new SpinnerTextureSequence(atlas.GetAtlasSubtextures(path),
                            SpinnerAnimationMetadata.GetForTexture(engine, path, next + "/fg", next + "/" + suffix,
                                next + "/")));
                    }

                    if (atlas.Has(next + "/bg" + suffix + "00"))
                    {
                        found = true;
                        string path = next + "/bg" + suffix;
                        background.Add(new SpinnerTextureSequence(atlas.GetAtlasSubtextures(path),
                            SpinnerAnimationMetadata.GetForTexture(engine, path, next + "/bg", next + "/" + suffix,
                                next + "/")));
                    }

                    if (!found) break;
                }

                if (foreground.Count == 0) AddAnimatedFallback(foreground, "fg");
                if (background.Count == 0) AddAnimatedFallback(background, "bg");
            }
            else
            {
                foreground = LoadStatic(ForegroundPath(false), true);
                background = LoadStatic(BackgroundPath(false), true);
            }

            if (HasDeco)
            {
                foregroundDecoration = LoadStatic(ForegroundPath(false) + "Deco", false);
                backgroundDecoration = LoadStatic(BackgroundPath(false) + "Deco", false);
            }

            var first = foreground[0].First;
            CullingDistance = new Vector2(first.FrameWidth + 8f, first.FrameHeight + 8f) / 2f;
            ConnectionWidth = first.FrameWidth / 12 * 12 + (first.FrameWidth % 12 > 0 ? 12 : 0);
        }

        private string ForegroundPath(bool hot) => Directory + (hot ? "/hot/fg" : "/fg") + SpritePathSuffix;
        private string BackgroundPath(bool hot) => Directory + (hot ? "/hot/bg" : "/bg") + SpritePathSuffix;

        private List<SpinnerTextureSequence> LoadStatic(string path, bool fallback)
        {
            var entries = atlas.GetAtlasSubtextures(path);
            var result = new List<SpinnerTextureSequence>(entries.Count);
            foreach (var entry in entries) result.Add(new SpinnerTextureSequence(entry));
            if (fallback && result.Count == 0)
            {
                Warn("No spinner sprites found, searched at: " + path);
                result.Add(new SpinnerTextureSequence(RequireFallback()));
            }

            return result;
        }

        private AtlasEntry RequireFallback()
            => atlas.GetFallback() ??
               throw new InvalidOperationException("Gameplay atlas has no actual fallback texture.");

        private void AddAnimatedFallback(List<SpinnerTextureSequence> list, string kind)
        {
            Warn("No " + kind + " Animated Spinner sprites found, searched at: " + Directory + "/00/" +
                 SpritePathSuffix + kind + "00.png");
            list.Add(new SpinnerTextureSequence(new List<AtlasEntry> { RequireFallback() },
                new SpinnerAnimationMetadata()));
        }

        private void Warn(string message) => ((IEverestLoggerProcessEnvironment)engine).Logger.Log(
            EverestLogLevel.Warn, "FrostHelper.SpinnerSprites", message);

        public List<SpinnerTextureSequence> GetFgTextures(bool hot)
            => hot ? hotForeground ??= LoadStatic(ForegroundPath(true), false) : foreground;

        // Deliberately preserve Frost's hot-bg lookup of the foreground path.
        public List<SpinnerTextureSequence> GetBgTextures(bool hot)
            => hot ? hotBackground ??= LoadStatic(ForegroundPath(true), false) : background;

        public List<SpinnerTextureSequence> GetDecoTextures(bool hot) => foregroundDecoration ??= new();
        public List<SpinnerTextureSequence> GetBgDecoTextures(bool hot) => backgroundDecoration ??= new();
    }
}
