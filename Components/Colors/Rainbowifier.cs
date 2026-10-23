// SPDX-License-Identifier: MIT

using Celeste.Core.Components;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Graphics.Animation;
using Celeste.Mods.FrostHelper.Runtime.Colors;

namespace Celeste.Mods.FrostHelper.Components.Colors
{
    [RuntimeTypeAlias("FrostHelper.Rainbowifier")]
    public sealed class Rainbowifier : SourceComponentState
    {
        private readonly EntityRainbowifyHooks hooks;
        public Rainbowifier(EntityRainbowifyHooks hooks) : base(false, true) => this.hooks = hooks;

        public override void Render(ISourceRenderContext context)
        {
            foreach (var component in ((ISourceOwnedComponentList)Entity).OwnedComponents)
                if (component is SpriteImage image)
                    image.Color = hooks.GetHue(Scene, image.RenderPosition);
        }
    }
}
