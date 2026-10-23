// SPDX-License-Identifier: MIT

using System;
using Celeste.Core.Components;
using Celeste.Core.Reflection.Runtime;

namespace Celeste.Mods.FrostHelper.Components.Lifecycle
{
    [RuntimeTypeAlias("FrostHelper.OnAwakeCallbackComponent")]
    public class OnAwakeCallbackComponent(Action onAwake) : SourceComponentState(false, false)
    {
        public override void EntityAwake()
        {
            base.EntityAwake();
            onAwake();
        }
    }
}
