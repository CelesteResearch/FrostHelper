using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Compilation;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Descriptors;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Conditions.Consts;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Operators;
using Session = Celeste.Simulation.Runtime.Session.ChapterCompletionSession;

namespace Celeste.Mods.FrostHelper.Runtime.Expressions.Conditions
{
    using Color = Celeste.Core.Display.PackedColor;


    internal sealed class OperatorInvert(Condition x) : Condition
    {
        private readonly Condition _innerCondition = x;

        private static readonly FieldInfo FieldInnerCondition =
            typeof(OperatorInvert).GetField(nameof(_innerCondition), BindingFlags.Instance | BindingFlags.NonPublic)!;

        public override object Get(Session session, object? userdata)
        {
            return CoerceToBool(_innerCondition.Get(session, userdata)) ? Zero : One;
        }

        internal override void Emit(ConditionCompilationCtx ctx, Type targetType)
        {
            LocalBuilder? temp = null;
            ctx.Il.EmitSwapOutCurrentCondition(ref temp, ctx, _innerCondition, FieldInnerCondition);
            _innerCondition.Emit(ctx, typeof(bool));
            ctx.Il.EmitRevertCurrentCondition(temp, ctx);

            ctx.Il.Emit(OpCodes.Ldnull);
            ctx.Il.Emit(OpCodes.Ceq);
            ctx.EmitConvertTo(typeof(bool), targetType);
        }

        public override bool OnlyChecksFlags() => _innerCondition.OnlyChecksFlags();

        protected internal override Type ReturnType => typeof(int);

        protected override IEnumerable<object> GetArgsForDebugPrint() => [_innerCondition];
    }
}
