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
    internal sealed class FieldInfoAccessor<T, TField> : FieldAccessorCommand
    {
        private readonly FieldInfo _fieldInfo;
        private readonly Func<T, TField> _getter;

        public FieldInfoAccessor(string sessionExpressionName, string fieldName, IReadOnlyList<RenderPart> description)
        {
            _fieldInfo = typeof(T).GetField(fieldName)!;
            _getter = ColdTypedGetter<T, TField>.ForField(typeof(T).GetField(fieldName)!);

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

        public override Type ReturnType => _fieldInfo.FieldType;

        public override void Emit(ConditionCompilationCtx ctx, Type? knownInputType, Type targetType)
        {
            ctx.Il.Emit(OpCodes.Ldfld, _fieldInfo);
            ctx.Il.EmitConvertToInSessionExpression(_fieldInfo.FieldType, targetType);
        }
    }
}
