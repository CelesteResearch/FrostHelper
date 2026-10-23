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
    public sealed class SpinnerDecoRenderer : SpinnerRenderer
    {
        public SpinnerDecoRenderer(ISourceEngineContext engine, int depth,
            Func<ICustomSpinnerRenderSettings> settings) : base(engine, depth, -10001, settings)
            => Active = false;

        public override void Render(ISourceRenderContext context)
        {
            foreach (var spinner in Items) spinner.RenderDecoration(context);
        }
    }
}
