// SPDX-License-Identifier: MIT

using System;
using Celeste.Everest.Content.Extensions.Decals;
using Celeste.Mods.FrostHelper.Content.Decals;
using Celeste.Mods.FrostHelper.Runtime.Decals;

namespace Celeste.Mods.FrostHelper.Modules
{
    public sealed partial class FrostHelperModule
    {
        private IDisposable decalContainerIgnoreRegistration;

        private void LoadDecalContainers()
        {
            var state = FrostDecalContainerState.For(SourceEngine);
            decalContainerIgnoreRegistration ??= ((IEverestDecalRegistryEnvironment)SourceEngine).DecalRegistry
                .RegisterPropertyHandler(() => new DecalContainerIgnoreRegistryHandler(state));
        }

        private void UnloadDecalContainers()
        {
            decalContainerIgnoreRegistration?.Dispose();
            decalContainerIgnoreRegistration = null;
        }
    }
}
