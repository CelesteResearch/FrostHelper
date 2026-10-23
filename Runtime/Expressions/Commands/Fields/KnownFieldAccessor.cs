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
    internal sealed class KnownFieldAccessor : Condition
    {
        private Condition _target;
        private readonly FieldAccessorCommand _accessor;

        public KnownFieldAccessor(Condition target, FieldAccessorCommand accessor)
        {
            _accessor = accessor;
            _target = target;
            Descriptor = _accessor.Descriptor;
        }

        public Condition Target => _target;

        private static readonly FieldInfo TargetField =
            typeof(KnownFieldAccessor).GetField(nameof(_target), BindingFlags.NonPublic | BindingFlags.Instance)!;

        public override object Get(Session session, object? userdata)
        {
            var t = _target.Get(session, userdata);

            return _accessor.GetValue(t);
        }

        internal override void Emit(ConditionCompilationCtx ctx, Type targetType)
        {
            var il = ctx.Il;
            LocalBuilder? tempLoc = null;
            il.EmitSwapOutCurrentCondition(ref tempLoc, ctx, _target, TargetField);

            _target.Emit(ctx, _target.ReturnType!);

            il.EmitRevertCurrentCondition(tempLoc, ctx);

            _accessor.Emit(ctx, _target.ReturnType, targetType);
        }

        internal override bool UsesCurrentConditionLocalInEmit => _target.UsesCurrentConditionLocalInEmit;

        protected internal override Type ReturnType => _accessor.ReturnType;
    }
}
