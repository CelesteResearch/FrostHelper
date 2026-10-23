using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Rendering;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Rendering.Effects;

namespace Celeste.Mods.FrostHelper.Spinners.Rendering
{
    public interface ICustomSpinnerRenderSource
    {
        bool Visible { get; }
        int Depth { get; }
        Vector2 Position { get; }
        Vector2 ShakeAmt { get; }
        float ImageScale { get; }
        PackedColor BorderColor { get; }
        float TimeUnpaused { get; }
        IReadOnlyList<SpinnerRenderImage> Sprites { get; }
        IReadOnlyList<SpinnerRenderImage> ConnectionFills { get; }
        void RenderDecoration(ISourceRenderContext context);
    }
}
