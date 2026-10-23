using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Conditions;
using Entity = Celeste.Scene.Entities.SceneEntityState;
using Player = Celeste.Simulation.RoomPlayerSession;
using EntityID = Celeste.Core.Maps.MapEntityId;
using Vector2 = System.Numerics.Vector2;

namespace Celeste.Mods.FrostHelper.Runtime.Expressions.Descriptors
{
    using Color = Celeste.Core.Display.PackedColor;

    public sealed class ArgumentDescriptor(string name, TypeDescriptor type)
    {
        public string Name { get; init; } = name;

        public TypeDescriptor Type { get; init; } = type;

        public static ArgumentDescriptor VarargFor(TypeDescriptor descriptor)
            => new ArgumentDescriptor("...", descriptor);
    }
}
