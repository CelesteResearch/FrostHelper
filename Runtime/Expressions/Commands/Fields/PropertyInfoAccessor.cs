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
    internal sealed class PropertyInfoAccessor<T, TField> : FieldAccessorCommand where T : class
    {
        private readonly PropertyInfo _propertyInfo;
        private readonly Func<T, TField> _getter;

        public PropertyInfoAccessor(string sessionExpressionName, string fieldName,
            IReadOnlyList<RenderPart> description)
        {
            _propertyInfo = typeof(T).GetProperty(fieldName)!;
            _getter =
                (Func<T, TField>)typeof(T).GetProperty(fieldName)!.GetGetMethod()!.CreateDelegate(
                    typeof(Func<T, TField>));

            Descriptor = new CommandDescriptor
            {
                Name = sessionExpressionName,
                DeclaringType = TypeDescriptor.For(typeof(T)),
                ReturnType = TypeDescriptor.For(typeof(TField)),
                Description = description,
            };
        }

        public override object GetValue(object? obj)
        {
            return _getter((T)obj!)!;
        }

        public override Type ReturnType => _propertyInfo.PropertyType;

        public override void Emit(ConditionCompilationCtx ctx, Type? knownInputType, Type targetType)
        {
            ctx.Il.Emit(OpCodes.Callvirt, _propertyInfo.GetMethod!);
            ctx.Il.EmitConvertToInSessionExpression(_propertyInfo.PropertyType, targetType);
        }
    }
}
