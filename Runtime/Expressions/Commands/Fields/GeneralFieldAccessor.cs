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
    internal sealed class GeneralFieldAccessor(string fieldName, Condition target, IExpressionContext ctx) : Condition
    {
        private FieldAccessorCommand? _permanentlyKnownFieldAccessor;
        private readonly Dictionary<Type, FieldAccessorCommand?> _cache = [];
        private readonly Condition _target = target;

        private static readonly FieldInfo TargetField =
            typeof(GeneralFieldAccessor).GetField("_target", BindingFlags.NonPublic | BindingFlags.Instance)!;

        public override object Get(Session session, object? userdata)
        {
            var t = _target.Get(session, userdata);

            if (_cache.TryGetValue(t.GetType(), out var cached))
            {
                return cached?.GetValue(t) ?? Zero;
            }

            if (ctx.Runtime.FieldAccessCommands.GetAccessor(t.GetType(), fieldName, ctx) is { } accessor)
            {
                _cache[t.GetType()] = accessor;
                return accessor.GetValue(t);
            }

            _cache[t.GetType()] = null;
            ctx.Runtime.Notify($"Failed to get field '{fieldName}' on type '{t.GetType().Name}'");

            return Zero;
        }

        internal override void Emit(ConditionCompilationCtx ctx, Type targetType)
        {
            var knownAccessor = GetPermanentlyKnownFieldAccessor();
            if (knownAccessor is null)
            {
                base.Emit(ctx, targetType);
                return;
            }

            var il = ctx.Il;
            LocalBuilder? tempLoc = null;
            il.EmitSwapOutCurrentCondition(ref tempLoc, ctx, _target, TargetField);

            _target.Emit(ctx, _target.ReturnType!);

            il.EmitRevertCurrentCondition(tempLoc, ctx);

            knownAccessor.Emit(ctx, _target.ReturnType, targetType);
        }

        internal override bool UsesCurrentConditionLocalInEmit
            => GetPermanentlyKnownFieldAccessor() is null || _target.UsesCurrentConditionLocalInEmit;

        private FieldAccessorCommand? GetPermanentlyKnownFieldAccessor()
        {
            if (_permanentlyKnownFieldAccessor is not null)
                return _permanentlyKnownFieldAccessor;

            var fieldType = _target.ReturnType;
            if (fieldType is null)
                return null;

            if (_cache.TryGetValue(fieldType, out var cachedAccessor))
            {
                return _permanentlyKnownFieldAccessor = cachedAccessor;
            }

            if (ctx.Runtime.FieldAccessCommands.GetAccessor(fieldType, fieldName, ctx) is { } accessor)
            {
                return _permanentlyKnownFieldAccessor = accessor;
            }

            return null;
        }

        protected internal override Type? ReturnType => GetPermanentlyKnownFieldAccessor()?.ReturnType;
    }
}
