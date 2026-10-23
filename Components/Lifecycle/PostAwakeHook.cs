using System;
using Celeste.Core.Components;
using Celeste.Core.Reflection.Runtime;
using Celeste.Scene.Tracking.Attributes;

namespace Celeste.Mods.FrostHelper.Components.Lifecycle
{
    [SceneTracked]
    [RuntimeTypeAlias("FrostHelper.Components.PostAwakeHook")]
    public sealed class PostAwakeHook : SourceComponentState
    {
        public readonly Action OnAwake;
        private bool awoken;

        public PostAwakeHook(Action onAwake) : base(true, false) => OnAwake = onAwake;

        internal void InvokeAfterAwake()
        {
            if (awoken) return;
            awoken = true;
            OnAwake();
        }

        public override void Update()
        {
            if (awoken) RemoveSelf();
        }
    }
}
