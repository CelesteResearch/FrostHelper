// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Graphics;
using Celeste.Graphics.Content;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Entities;
using Celeste.Scene.Scheduling;
using Celeste.Scene.Tracking;
using Celeste.Simulation.Presentation.Decals.Entities;
using Celeste.Simulation.Presentation.Lighting;
using Celeste.Simulation.Presentation.PostProcessing.Bloom;
using Celeste.Simulation.Gameplay.Platforms.StaticMovers;

namespace Celeste.Mods.FrostHelper.Entities.Decals
{
    internal static class DecalContainerSceneBinding
    {
        internal static void SetScene(SourceDecalEntityState decal, object scene)
        {
            ((ISceneEntityAssociation)decal).Scene = scene;
            foreach (var component in decal) component.SetRawEntity(decal);
        }
    }
}
