// SPDX-License-Identifier: MIT

using System.Xml;
using Celeste.Everest.Content.Extensions.Decals;
using Celeste.Simulation.Presentation.Decals.Entities;
using Celeste.Mods.FrostHelper.Runtime.Decals;

namespace Celeste.Mods.FrostHelper.Content.Decals
{
    internal sealed class DecalContainerIgnoreRegistryHandler(FrostDecalContainerState state)
        : EverestDecalPropertyHandler
    {
        public override string Name => "frosthelper.decalContainerIgnore";

        public override void Parse(XmlAttributeCollection xml)
        {
            // The original property has no parameters.
        }

        public override void ApplyTo(SourceDecalEntityState decal) => state.IgnoredNames.Add(decal.Name);
    }
}
