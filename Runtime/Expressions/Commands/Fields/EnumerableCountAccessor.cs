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
    internal sealed class EnumerableCountAccessor : FieldAccessorCommand
    {
        public EnumerableCountAccessor()
        {
            Descriptor = new CommandDescriptor
            {
                Name = "count",
                DeclaringType = TypeDescriptor.For(typeof(IEnumerable)),
                ReturnType = TypeDescriptor.For(typeof(int)),
                Description =
                [
                    RenderPart.Default("Returns the amount of elements in this "),
                    RenderPart.Type(TypeDescriptor.For(typeof(IEnumerable))),
                    RenderPart.Default(".")
                ],
            };
        }

        public override object GetValue(object? obj)
        {
            return ((IEnumerable?)obj)?.Cast<object>().Count() ?? 0;
        }

        public override Type ReturnType => typeof(int);

        private static readonly MethodInfo Linq_Enumerable_Cast_T =
            typeof(Enumerable).GetMethod(nameof(Enumerable.Cast))!;

        private static readonly MethodInfo Linq_Enumerable_Count_T = typeof(Enumerable).GetMethods()
            .Single(m => m.Name == nameof(Enumerable.Count) && m.GetParameters().Length == 1);

        public override void Emit(ConditionCompilationCtx ctx, Type? knownInputType, Type targetType)
        {
            if (knownInputType?.GetProperty("Count") is { GetMethod: { } getCount } countProperty &&
                countProperty.PropertyType == typeof(int))
            {
                ctx.Il.Emit(OpCodes.Call, getCount);
            }
            else
            {
                ctx.Il.Emit(OpCodes.Call, Linq_Enumerable_Cast_T.MakeGenericMethod(typeof(object)));
                ctx.Il.Emit(OpCodes.Call, Linq_Enumerable_Count_T.MakeGenericMethod(typeof(object)));
            }

            ctx.EmitConvertTo(typeof(int), targetType);
        }
    }
}
