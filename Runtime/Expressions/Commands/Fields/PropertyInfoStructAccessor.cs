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
    internal sealed class PropertyInfoStructAccessor<T, TField, TRetAs> : FieldAccessorCommand where T : struct
    {
        delegate TField RefFunc(ref T value);

        private readonly FrostConditionRuntime runtime;
        private readonly PropertyInfo _propertyInfo;
        private readonly RefFunc _getter;

        public PropertyInfoStructAccessor(FrostConditionRuntime runtime, string sessionExpressionName, string fieldName,
            IReadOnlyList<RenderPart> description)
        {
            this.runtime = runtime;
            _propertyInfo = typeof(T).GetProperty(fieldName)!;
            _getter = (RefFunc)typeof(T).GetProperty(fieldName)!.GetGetMethod()!.CreateDelegate(typeof(RefFunc));

            Descriptor = new CommandDescriptor
            {
                Name = sessionExpressionName,
                DeclaringType = TypeDescriptor.For(typeof(T)),
                ReturnType = TypeDescriptor.For(typeof(TRetAs)),
                Description = description,
            };
        }

        public override object GetValue(object? obj)
        {
            T val = (T)obj!;
            return Condition.Coerce<TRetAs>(_getter(ref val)!, runtime)!;
        }

        public override Type ReturnType => typeof(TRetAs);

        public override void Emit(ConditionCompilationCtx ctx, Type? knownInputType, Type targetType)
        {
            var loc = ctx.Il.DeclareLocal(typeof(T));
            ctx.Il.Emit(OpCodes.Stloc, loc);
            ctx.Il.Emit(OpCodes.Ldloca, loc);
            ctx.Il.Emit(OpCodes.Call, _propertyInfo.GetMethod!);
            ctx.Il.EmitConvertToInSessionExpression(_propertyInfo.PropertyType, targetType);
        }
    }
}
