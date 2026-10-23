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
    public sealed class SpinnerConnectorRenderer : SpinnerRenderer
    {
        public SpinnerConnectorRenderer(ISourceEngineContext engine, int depth,
            Func<ICustomSpinnerRenderSettings> settings) : base(engine, depth, depth + 1, settings)
            => Active = false;

        public override void Render(ISourceRenderContext context)
        {
            if (Items.Count == 0) return;
            var settings = Settings();
            if (settings.CanUseRenderTargetRender && settings.CanUseBlackOutlineRenderTargetOpt) return;
            ForceRender();
        }

        public void ForceRender(PackedColor? color = null)
        {
            foreach (var spinner in Items)
            {
                var fills = spinner.ConnectionFills;
                if (!spinner.Visible || fills.Count == 0) continue;
                // Frost batches each fill list using its first animated frame and centered origin.
                var texture = fills[0].TextureAt(spinner.TimeUnpaused);
                for (int i = 0; i < fills.Count; i++)
                {
                    var fill = fills[i];
                    DrawImage(spinner, fill, texture, color ?? fill.Color, Vector2.Zero, true);
                }
            }
        }
    }
}
