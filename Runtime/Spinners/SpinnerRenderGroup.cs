// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.FrostHelper.Entities.Spinners;
using Celeste.Mods.FrostHelper.Spinners.Rendering;

namespace Celeste.Mods.FrostHelper.Runtime.Spinners
{
    public sealed class SpinnerRenderGroup
    {
        private readonly OriginalLevelSceneRoot level;
        private readonly List<CustomSpinner> connectors = [];
        private readonly List<CustomSpinner> borders = [];
        private readonly List<CustomSpinner> decorations = [];
        public int Depth { get; }
        public SpinnerConnectorRenderer Connector { get; }
        public SpinnerBorderRenderer Border { get; }
        public SpinnerDecoRenderer Decoration { get; private set; }

        public SpinnerRenderGroup(OriginalLevelSceneRoot level, int depth)
        {
            this.level = level;
            Depth = depth;
            Connector = new SpinnerConnectorRenderer(level.EngineContext, depth, () => connectors[0].Controller);
            Border = new SpinnerBorderRenderer(level.EngineContext, depth, () => borders[^1].Controller, Connector);
            level.Scheduler.QueueSourceEntity(Connector);
            level.Scheduler.QueueSourceEntity(Border);
        }

        public void EnsureDecorationRenderer()
        {
            if (Decoration != null) return;
            Decoration = new SpinnerDecoRenderer(level.EngineContext, Depth, () => decorations[0].Controller);
            level.Scheduler.QueueSourceEntity(Decoration);
        }

        public void Register(CustomSpinner spinner)
        {
            if (spinner.RegisteredToRenderers) return;
            connectors.Add(spinner);
            Connector.Add(spinner);
            if (spinner.RenderBorder)
            {
                borders.Add(spinner);
                Border.Add(spinner);
            }

            if (spinner.SpriteSource.HasDeco)
            {
                EnsureDecorationRenderer();
                decorations.Add(spinner);
                Decoration.Add(spinner);
            }

            spinner.RegisteredToRenderers = true;
        }

        public void Unregister(CustomSpinner spinner)
        {
            if (!spinner.RegisteredToRenderers) return;
            connectors.Remove(spinner);
            Connector.Remove(spinner);
            borders.Remove(spinner);
            Border.Remove(spinner);
            decorations.Remove(spinner);
            Decoration?.Remove(spinner);
            spinner.RegisteredToRenderers = false;
        }
    }
}
