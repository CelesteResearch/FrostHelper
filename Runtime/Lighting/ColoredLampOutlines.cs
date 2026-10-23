// SPDX-License-Identifier: MIT

using System.Runtime.CompilerServices;
using Celeste.Core.Scheduling;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.VirtualAssets;
using Celeste.Graphics.Effects.Outlines;
using Celeste.Graphics.Rendering.Devices;

namespace Celeste.Mods.FrostHelper.Runtime.Lighting
{
    internal static class ColoredLampOutlines
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, Holder> caches = new();

        internal static Holder Bind(ISourceEngineContext engine) => caches.GetValue(engine, CreateHolder);

        private static Holder CreateHolder(ISourceEngineContext engine) => new(engine);

        internal sealed class Holder
        {
            private readonly ISourceEngineContext engine;
            private CachedImageOutline cache;

            internal Holder(ISourceEngineContext engine) => this.engine = engine;

            internal CachedImageOutline Value => cache ??= Create(engine);

            internal void Dispose()
            {
                var previous = cache;
                cache = null;
                previous?.Dispose();
            }
        }

        private static CachedImageOutline Create(ISourceEngineContext engine)
        {
            var atlas = ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas;
            return new CachedImageOutline(((ISourceVirtualContentEnvironment)engine).VirtualContent,
                ((ISourceGraphicsDeviceEnvironment)engine).GraphicsDevice, entry => atlas[entry.AtlasPath]);
        }

        internal static void Dispose(ISourceEngineContext engine)
        {
            if (!caches.TryGetValue(engine, out var cache)) return;
            cache.Dispose();
        }
    }
}
