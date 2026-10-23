using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Diagnostics.CodeAnalysis;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Operators;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Conditions;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Conditions.Consts;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Compilation;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Syntax;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Contexts;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Descriptors;
using Vector2 = System.Numerics.Vector2;
using Session = Celeste.Simulation.Runtime.Session.ChapterCompletionSession;

namespace Celeste.Mods.FrostHelper.Runtime.Expressions.Conditions
{
    using Color = Celeste.Core.Display.PackedColor;


    internal sealed class IndirectCounterAccessor(Condition nameCond) : Condition
    {
        private readonly Condition _nameCondition = nameCond;

        private static readonly FieldInfo FieldNameCondition =
            typeof(IndirectCounterAccessor).GetField(nameof(_nameCondition),
                BindingFlags.Instance | BindingFlags.NonPublic)
            !;

        private static readonly MethodInfo MethodSessionGetCounter =
            typeof(Session).GetMethod(nameof(Session.GetCounter), BindingFlags.Instance | BindingFlags.Public)!;

        public override object Get(Session session, object? userdata)
        {
            var name = _nameCondition.GetString(session, userdata);

            return session.GetCounter(name);
        }

        internal override void Emit(ConditionCompilationCtx ctx, Type targetType)
        {
            LocalBuilder? temp = null;
            ctx.EmitLoadSession();

            ctx.EmitSwapOutCurrentCondition(ref temp, _nameCondition, FieldNameCondition);
            _nameCondition.Emit(ctx, typeof(string));
            ctx.EmitRevertCurrentCondition(temp);

            ctx.Il.Emit(OpCodes.Callvirt, MethodSessionGetCounter);
            ctx.EmitConvertTo(typeof(int), targetType);
        }

        internal override bool UsesCurrentConditionLocalInEmit => _nameCondition.UsesCurrentConditionLocalInEmit;

        protected internal override Type ReturnType => typeof(int);

        protected override IEnumerable<object> GetArgsForDebugPrint() => [_nameCondition];
    }
}
