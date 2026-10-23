// SPDX-License-Identifier: MIT

using System;
using System.Linq;
using Celeste.Core.Components;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Runtime;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.FrostHelper.Modules;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation.Runtime.Session;

namespace Celeste.Mods.FrostHelper.Components.Flags
{
    [SceneTracked(false)]
    public sealed class FlagListener : SourceComponentState
    {
        public Action<ChapterCompletionSession, string, bool> OnSet;
        public string Flag;
        public bool MustChange;
        public bool TriggerOnRoomBegin;

        public FlagListener(ISourceEngineContext engine, string flag,
            Action<ChapterCompletionSession, string, bool> onSet, bool mustChange, bool triggerOnRoomBegin)
            : base(false, false)
        {
            ((ISourceEverestModulesEnvironment)engine).Modules.OfType<FrostHelperModule>().Single().EnsureFlagHook();
            Flag = flag;
            OnSet = onSet;
            MustChange = mustChange;
            TriggerOnRoomBegin = triggerOnRoomBegin;
        }

        public override void EntityAwake()
        {
            base.EntityAwake();
            if (!TriggerOnRoomBegin) return;
            var session = ((OriginalLevelSceneRoot)Scene).Session;
            OnSet(session, Flag, session.GetFlag(Flag));
        }
    }
}
