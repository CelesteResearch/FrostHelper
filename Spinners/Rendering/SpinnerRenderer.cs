using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Components;
using Celeste.Core.Rendering;
using Celeste.Core.Rendering.States;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Startup;
using Celeste.Game.Presentation.Rendering;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Content.VirtualAssets;
using Celeste.Graphics.Effects.Outlines;
using Celeste.Graphics.Rendering;
using Celeste.Graphics.Rendering.Batches;
using Celeste.Graphics.Rendering.Devices;
using Celeste.Graphics.Rendering.Drawing;
using Celeste.Scene.Entities;

namespace Celeste.Mods.FrostHelper.Spinners.Rendering
{
    public abstract class SpinnerRenderer : SceneEntityState
    {
        protected readonly ISourceEngineContext Engine;
        protected readonly List<ICustomSpinnerRenderSource> Items = new();
        protected readonly Func<ICustomSpinnerRenderSettings> Settings;
        protected SourceSpriteBatch Batch => ((ISourceDrawScope)Engine).Draw.SpriteBatch;
        public int BaseDepth { get; }
        public int Count => Items.Count;

        protected SpinnerRenderer(ISourceEngineContext engine, int depth, int renderDepth,
            Func<ICustomSpinnerRenderSettings> settings)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration)
        {
            Engine = engine;
            Settings = settings;
            BaseDepth = depth;
            Depth = renderDepth;
            Tags = ((IOriginalGameTagsEnvironment)engine).Tags.Persistent.Value;
        }

        public void Add(ICustomSpinnerRenderSource item) => Items.Add(item);
        public void Remove(ICustomSpinnerRenderSource item) => Items.Remove(item);

        protected void DrawImage(ICustomSpinnerRenderSource spinner, SpinnerRenderImage image,
            AtlasEntry texture, PackedColor color, Vector2 shift, bool centerOrigin)
        {
            float fix = texture.SourceDrawScale;
            Vector2 origin = ((centerOrigin ? texture.Center : image.Origin) - texture.DrawOffset) / fix;
            Batch.Draw(texture.ReadTextureForSpriteBatch(),
                spinner.Position + (centerOrigin ? spinner.ShakeAmt : Vector2.Zero) + image.Position + shift,
                texture.ClipRect, color.PackedValue, image.Rotation, origin,
                fix * spinner.ImageScale, SourceSpriteEffects.None, 0f);
        }

        protected void DrawForeground(ICustomSpinnerRenderSource spinner, PackedColor? overrideColor,
            float time)
        {
            var images = spinner.Sprites;
            for (int i = 0; i < images.Count; i++)
            {
                var image = images[i];
                DrawImage(spinner, image, image.TextureAt(time), overrideColor ?? image.Color,
                    Vector2.Zero, false);
            }
        }
    }
}
