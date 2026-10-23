// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.FrostHelper.Entities.Spinners;

namespace Celeste.Mods.FrostHelper.Runtime.Spinners
{
    public sealed class SpinnerSceneGroups
    {
        private readonly OriginalLevelSceneRoot level;
        private readonly Dictionary<int, SpinnerRenderGroup> groups = [];
        private CustomSpinnerController controller;

        public SpinnerSceneGroups(OriginalLevelSceneRoot level) => this.level = level;

        public SpinnerRenderGroup Get(int depth)
        {
            if (groups.TryGetValue(depth, out var group) &&
                (ReferenceEquals(group.Border.Scene, level) ||
                 level.Scheduler.GetToAdd().Contains(group.Border)) &&
                (ReferenceEquals(group.Connector.Scene, level) ||
                 level.Scheduler.GetToAdd().Contains(group.Connector)))
                return group;
            // Reload and teleport remove Persistent entities without replacing the level scene.
            // A depth cache must not retain renderers detached by that unload.
            group = new SpinnerRenderGroup(level, depth);
            groups[depth] = group;
            return group;
        }

        public CustomSpinnerController GetController()
        {
            string room = level.Session.Level;
            if (controller != null && controller.LevelName == room &&
                (ReferenceEquals(controller.Scene, level) || level.Scheduler.GetToAdd().Contains(controller)))
                return controller;
            foreach (object entity in level.Scheduler.Entities.Entities)
            {
                if (entity is CustomSpinnerController found && found.LevelName == room)
                    return controller = found;
            }

            foreach (object entity in level.Scheduler.GetToAdd())
            {
                if (entity is CustomSpinnerController found && found.LevelName == room)
                    return controller = found;
            }

            controller = new CustomSpinnerController(level.EngineContext, room);
            level.Scheduler.QueueSourceEntity(controller);
            return controller;
        }
    }
}
