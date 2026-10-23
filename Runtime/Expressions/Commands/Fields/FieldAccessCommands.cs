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
        private readonly FrostConditionRuntime runtime;

        public FieldAccessCommands(FrostConditionRuntime runtime)
        {
            this.runtime = runtime;
            Accessors = new();

            RegisterField<Vector2, float, Vector2XAccessor>("x",
                "The position of the vector on the x-axis, e.g. $vec(3, 4).x -> 3");
            RegisterField<Vector2, float, Vector2YAccessor>("y",
                "The position of the vector on the y-axis, e.g. $vec(3, 4).y -> 4");
            RegisterField<Vector2, float, Vector2LenAccessor>("len",
                "The length of the vector, calculated as $sqrt(x*x + y*y)");
            RegisterField<Vector2, float, Vector2LenSqAccessor>("lenSq",
                "The squared length of the vector, calculated as x*x + y*y. Faster to calculate than .len.");
            RegisterField<Vector2, float, Vector2AngleAccessor>("angle", "The angle of this vector, in radians.");

            RegisterField<string, int, StringLenAccessor>("len",
                "The length (in characters) of the string, e.g. \"hello\".len -> 5");

            RegisterProperty<Entity, float>("x", nameof(Entity.X), "The position of the entity on the x-axis.");
            RegisterProperty<Entity, float>("y", nameof(Entity.Y), "The position of the entity on the y-axis.");
            RegisterProperty<Entity, Vector2>("pos", nameof(Entity.Position), "The position of the entity.");
            RegisterSid("sid",
                "The SID (String ID, used in map editors) of the entity, e.g. FrostHelper/IceSpinner for Custom Spinners. Can be ? if the SID cannot be obtained for the given entity.");

            RegisterField<Player, IEnumerable<Entity>, PlayerFollowersAccessor>("followers",
                "Gets all followers of this player, like berries or keys.");

            RegisterField<EntityID, string>("roomName", nameof(EntityID.Level),
                "The name of the room the ID belongs to.");
            RegisterField<EntityID, int>("id", nameof(EntityID.ID), "The numerical ID, unique per-room.");

            RegisterStructProperty<PackedColor, byte, int>("r", nameof(PackedColor.R),
                "The Red channel of this color, between 0 and 255 inclusive.");
            RegisterStructProperty<PackedColor, byte, int>("g", nameof(PackedColor.G),
                "The Green channel of this color, between 0 and 255 inclusive.");
            RegisterStructProperty<PackedColor, byte, int>("b", nameof(PackedColor.B),
                "The Blue channel of this color, between 0 and 255 inclusive.");
            RegisterStructProperty<PackedColor, byte, int>("a", nameof(PackedColor.A),
                "The Alpha channel of this color, between 0 and 255 inclusive.");

            Accessors[(typeof(IEnumerable), "count")] = new EnumerableCountAccessor();
        }

        internal readonly Dictionary<(Type, string), FieldAccessorCommand> Accessors;

        private void RegisterField<T, TField, TImpl>(string name, string description)
            where TImpl : IFieldAccessor<T, TField>
            => RegisterField<T, TField, TImpl>(name, [RenderPart.Default(description)]);

        private void RegisterField<T, TField, TImpl>(string name, IReadOnlyList<RenderPart> description)
            where TImpl : IFieldAccessor<T, TField>
        {
            Accessors[(typeof(T), name)] = new FieldAccessor<T, TField, TImpl>(name, description);
        }

        private void RegisterField<T, TField>(string name, string fieldNameCSharp, string description)
            => RegisterField<T, TField>(name, fieldNameCSharp, [RenderPart.Default(description)]);

        private void RegisterField<T, TField>(string name, string fieldNameCSharp,
            IReadOnlyList<RenderPart> description)
        {
            Accessors[(typeof(T), name)] = new FieldInfoAccessor<T, TField>(name, fieldNameCSharp, description);
        }

        private void RegisterProperty<T, TField>(string name, string propertyNameCSharp, string description)
            where T : class
            => RegisterProperty<T, TField>(name, propertyNameCSharp, [RenderPart.Default(description)]);

        private void RegisterProperty<T, TField>(string name, string propertyNameCSharp,
            IReadOnlyList<RenderPart> description)
            where T : class
        {
            Accessors[(typeof(T), name)] = new PropertyInfoAccessor<T, TField>(name, propertyNameCSharp, description);
        }

        private void RegisterStructProperty<T, TField, TRetAs>(string name, string propertyNameCSharp,
            string description)
            where T : struct
            => RegisterStructProperty<T, TField, TRetAs>(name, propertyNameCSharp, [RenderPart.Default(description)]);

        private void RegisterStructProperty<T, TField, TRetAs>(string name, string propertyNameCSharp,
            IReadOnlyList<RenderPart> description)
            where T : struct
        {
            Accessors[(typeof(T), name)] =
                new PropertyInfoStructAccessor<T, TField, TRetAs>(runtime, name, propertyNameCSharp, description);
        }

        internal Condition Create(string fieldName, Condition target, IExpressionContext ctx)
        {
            if (target.ReturnType is { } knownType && GetAccessor(knownType, fieldName, ctx) is { } accessor)
            {
                return new KnownFieldAccessor(target, accessor)
                {
                    Runtime = ctx.Runtime,
                    SourceText = $".{fieldName}"
                };
            }

            return new GeneralFieldAccessor(fieldName, target, ctx)
            {
                Runtime = ctx.Runtime,
                SourceText = $".{fieldName}"
            };
        }

        internal FieldAccessorCommand? GetAccessor(Type? type, string fieldName, IExpressionContext ctx)
        {
            var currentType = type;
            while (currentType is not null)
            {
                if (Accessors.TryGetValue((currentType, fieldName), out var accessor))
                {
                    if (currentType != type)
                    {
                        Accessors[(type!, fieldName)] = accessor;
                    }

                    return accessor;
                }

                foreach (var interfaceType in currentType.GetInterfaces())
                {
                    if (Accessors.TryGetValue((interfaceType, fieldName), out accessor))
                    {
                        if (currentType != type)
                        {
                            Accessors[(type!, fieldName)] = accessor;
                        }

                        return accessor;
                    }
                }

                currentType = currentType.BaseType;
            }

            return null;
        }
    }
}
