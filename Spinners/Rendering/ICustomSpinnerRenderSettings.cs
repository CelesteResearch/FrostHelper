using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Rendering;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Rendering.Effects;

namespace Celeste.Mods.FrostHelper.Spinners.Rendering
{
    public interface ICustomSpinnerRenderSettings
    {
        bool CanUseRenderTargetRender { get; }
        bool CanUseBlackOutlineRenderTargetOpt { get; }
        PackedColor? FirstBorderColor { get; }
        SourceEffect OutlineShader { get; }
    }
}
