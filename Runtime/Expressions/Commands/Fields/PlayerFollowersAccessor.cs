using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;
using Celeste.Mods.FrostHelper.Runtime.Expressions;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Conditions;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Compilation;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Descriptors;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Contexts;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Commands.Binding;
using Session = Celeste.Simulation.Runtime.Session.ChapterCompletionSession;
using Entity = Celeste.Scene.Entities.SceneEntityState;
using Player = Celeste.Simulation.RoomPlayerSession;
using EntityID = Celeste.Core.Maps.MapEntityId;
using PackedColor = Celeste.Core.Display.PackedColor;

namespace Celeste.Mods.FrostHelper.Runtime.Expressions.Commands
{
    internal struct PlayerFollowersAccessor : IFieldAccessor<Player, IEnumerable<Entity>>
    {
        public static IEnumerable<Entity> GetValue(Player? player)
        {
            if (player is null)
                return Array.Empty<Entity>();

            return player.Leader.Followers.Select(f => (Entity)f.Entity);
        }
    }
}
