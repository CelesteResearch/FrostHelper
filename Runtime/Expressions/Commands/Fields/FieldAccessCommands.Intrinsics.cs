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
    internal sealed partial class FieldAccessCommands
    {
        private struct StringLenAccessor : IFieldAccessor<string, int>
        {
            public static int GetValue(string? obj)
            {
                return obj?.Length ?? 0;
            }
        }

        private struct Vector2LenAccessor : IFieldAccessor<Vector2, float>
        {
            public static float GetValue(Vector2 obj)
            {
                return obj.Length();
            }
        }

        private struct Vector2LenSqAccessor : IFieldAccessor<Vector2, float>
        {
            public static float GetValue(Vector2 obj)
            {
                return obj.LengthSquared();
            }
        }

        private struct Vector2AngleAccessor : IFieldAccessor<Vector2, float>
        {
            public static float GetValue(Vector2 obj)
            {
                return (float)Math.Atan2(obj.Y, obj.X);
            }
        }

        private struct Vector2XAccessor : IFieldAccessor<Vector2, float>
        {
            public static float GetValue(Vector2 obj)
            {
                return obj.X;
            }
        }

        private struct Vector2YAccessor : IFieldAccessor<Vector2, float>
        {
            public static float GetValue(Vector2 obj)
            {
                return obj.Y;
            }
        }

        private void RegisterSid(string name, string description)
        {
            Accessors[(typeof(Entity), name)] = new EntitySidAccessor(runtime, name, description);
        }
    }
}
