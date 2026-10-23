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

namespace Celeste.Mods.FrostHelper.Runtime.Expressions.Operators
{
    using Color = Celeste.Core.Display.PackedColor;

    internal sealed class OperatorAnd(Condition a, Condition b)
        : Condition
    {
        private readonly Condition _a = a, _b = b;

        private static readonly FieldInfo
            FieldA = typeof(OperatorAnd).GetField(nameof(_a), BindingFlags.Instance | BindingFlags.NonPublic)!,
            FieldB = typeof(OperatorAnd).GetField(nameof(_b), BindingFlags.Instance | BindingFlags.NonPublic)!;

        public override object Get(Session session, object? userdata)
        {
            return CoerceToBool(_a.Get(session, userdata)) && CoerceToBool(_b.Get(session, userdata)) ? 1 : 0;
        }

        internal override void Emit(ConditionCompilationCtx ctx, Type targetType)
        {
            LocalBuilder? temp = null;
            var il = ctx.Il;
            var falseLabel = il.DefineLabel();
            var endLabel = il.DefineLabel();

            il.EmitSwapOutCurrentCondition(ref temp, ctx, _a, FieldA);
            _a.Emit(ctx, typeof(bool));
            il.Emit(OpCodes.Brfalse, falseLabel);

            il.EmitSwapOutCurrentCondition(ref temp, ctx, _b, FieldB);
            _b.Emit(ctx, typeof(bool));
            il.Emit(OpCodes.Br, endLabel);

            il.MarkLabel(falseLabel);
            il.Emit(OpCodes.Ldc_I4_0);

            il.MarkLabel(endLabel);

            il.EmitRevertCurrentCondition(temp, ctx);

            ctx.EmitConvertTo(typeof(bool), targetType);
        }

        internal override bool UsesCurrentConditionLocalInEmit =>
            _a.UsesCurrentConditionLocalInEmit || _b.UsesCurrentConditionLocalInEmit;

        public override bool OnlyChecksFlags() => _a.OnlyChecksFlags() && _b.OnlyChecksFlags();

        protected internal override Type ReturnType => typeof(int);

        protected override IEnumerable<object> GetArgsForDebugPrint() => [_a, _b];
    }
}
