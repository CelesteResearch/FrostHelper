// SPDX-License-Identifier: MIT

using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Mods.FrostHelper.Components.Flags;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Runtime.Session;

namespace Celeste.Mods.FrostHelper.Triggers.Activators
{
    [CustomEntity("FrostHelper/OnFlagActivator")]
    [RuntimeTypeAlias("FrostHelper.Triggers.Activator.OnFlagActivator")]
    internal sealed class OnFlagActivator : BaseActivator
    {
        public readonly bool TargetValue;

        public OnFlagActivator(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(engine, data, offset)
        {
            Collidable = false;
            Add(new FlagListener(engine, data.Attr("flag"), OnFlag, data.Bool("mustChange"),
                data.Bool("triggerOnRoomBegin")));
            TargetValue = data.Bool("targetState", true);
            ActivateAfterDeath = data.Bool("activateAfterDeath", true);
        }

        public override void Awake(object scene)
        {
            Components.LockMode = SourceOwnedComponentList.LockModes.Locked;
            base.Awake(scene);
            Components.LockMode = SourceOwnedComponentList.LockModes.Open;
        }

        private void OnFlag(ChapterCompletionSession session, string flag, bool value)
        {
            if (value == TargetValue)
            {
                var player = Scene == null
                    ? null
                    : ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntity<RoomPlayerSession>();
                if (player != null || ActivateAfterDeath)
                    ActivateAll(player);
            }
            else
                CallOnLeave();
        }

        public override void OnEnter(RoomPlayerSession player)
        {
            base.OnEnter(player);
            ActivateAll(player);
        }

        public override void Update()
        {
            base.Update();
            CallOnStay();
        }
    }
}
