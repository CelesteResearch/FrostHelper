// SPDX-License-Identifier: MIT

using System;
using Celeste.Core.Components;
using Celeste.Core.Reflection.Runtime;
using Celeste.Mods.FrostHelper.Runtime.Colors;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;

namespace Celeste.Mods.FrostHelper.Components.Colors
{
    [SceneTracked(false)]
    [RuntimeTypeAlias("FrostHelper.Components.AnyEntityAddedHook")]
    public sealed class AnyEntityAddedHook : SourceComponentState
    {
        public readonly Action<SceneEntityState, object> OnEntityAdded;

        // The original _awoken is never assigned: this component persists for late additions.
        public bool CatchUpToPreviouslyAdded { get; }

        public AnyEntityAddedHook(EntityRainbowifyHooks hooks, Action<SceneEntityState, object> callback,
            bool catchUpToPreviouslyAdded = false) : base(true, false)
        {
            OnEntityAdded = callback;
            CatchUpToPreviouslyAdded = catchUpToPreviouslyAdded;
            hooks.EnsureAddedHook();
        }

        public override void EntityAdded(object scene)
        {
            base.EntityAdded(scene);
            if (CatchUpToPreviouslyAdded)
                foreach (SceneEntityState entity in ((ISourceScene)scene).SourceScene.Entities.Entities)
                    OnEntityAdded(entity, scene);
        }
    }
}
