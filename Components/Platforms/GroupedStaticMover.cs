// SPDX-License-Identifier: MIT

using System;
using Celeste.Everest.Tracking.Attributes;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Platforms.StaticMovers;

namespace Celeste.Mods.FrostHelper.Components.Platforms
{
    [SceneTracked]
    [EverestTrackedAs(typeof(SourceStaticMoverState))]
    public sealed class GroupedStaticMover : SourceStaticMoverState
    {
        public int Group;
        public bool CanBeLeader;

        public GroupedStaticMover(int group, bool canBeLeader)
        {
            Group = group;
            CanBeLeader = canBeLeader;
            OnAttach = CanBeLeader ? TryGroupAttach : null;
        }

        public GroupedStaticMover SetOnAttach(Action<ISourcePlatformEntity> callback)
        {
            if (callback != null) OnAttach += callback;
            return this;
        }

        private void TryGroupAttach(ISourcePlatformEntity platform)
        {
            var entity = (SceneEntityState)Entity;
            var leader = entity.Get<GroupedStaticMover>();
            if (leader == null || !leader.CanBeLeader || ReferenceEquals(entity, platform)) return;
            var scene = ((ISourceScene)Scene).SourceScene;
            foreach (var component in scene.Components.GetTrackedComponents(typeof(GroupedStaticMover)
                         .AssemblyQualifiedName))
            {
                var mover = (GroupedStaticMover)component;
                if (mover.Group != leader.Group || mover.Platform != null) continue;
                mover.OnAttach = null;
                mover.AttachToPlatform((SourcePlatformEntityState)platform);
            }
        }
    }
}
