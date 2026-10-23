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


    internal sealed class FlagAccessor(Condition nameCond, bool inverted) : Condition, IInvertible
    {
        public string? Flag => _nameCondition is ConstString c ? c.Value : null;

        private readonly Condition _nameCondition = nameCond;

        private static readonly FieldInfo FieldNameCondition =
            typeof(FlagAccessor).GetField(nameof(_nameCondition), BindingFlags.Instance | BindingFlags.NonPublic)!;

        public bool Inverted => inverted;

        public override bool OnlyChecksFlags() => _nameCondition.OnlyChecksFlags();

        private static readonly MethodInfo MethodSessionGetFlag = typeof(Session).GetMethod(nameof(Session.GetFlag))!;

        internal override void Emit(ConditionCompilationCtx ctx, Type targetType)
        {
            var il = ctx.Il;

            ctx.EmitLoadSession();
            if (_nameCondition is IConstCondition<string> constStr)
            {
                il.Emit(OpCodes.Ldstr, constStr.Value);
            }
            else
            {
                LocalBuilder? temp = null;
                ctx.Il.EmitSwapOutCurrentCondition(ref temp, ctx, _nameCondition, FieldNameCondition);

                _nameCondition.Emit(ctx, typeof(string));
                ctx.Il.EmitRevertCurrentCondition(temp, ctx);
            }

            il.Emit(OpCodes.Callvirt, MethodSessionGetFlag);
            if (Inverted)
            {
                il.Emit(OpCodes.Ldnull);
                il.Emit(OpCodes.Ceq);
            }

            il.EmitConvertToInSessionExpression(typeof(bool), targetType);
        }

        internal override bool UsesCurrentConditionLocalInEmit => _nameCondition.UsesCurrentConditionLocalInEmit;

        protected internal override Type ReturnType => typeof(int);

        protected override IEnumerable<object> GetArgsForDebugPrint() =>
        [
            Inverted ? $"!{Flag ?? _nameCondition.ToString()}" : Flag ?? _nameCondition.ToString()
        ];

        public Condition CreateInverted()
        {
            return new FlagAccessor(_nameCondition, !Inverted);
        }

        public override object Get(Session session, object? userdata)
        {
            var flag = Flag ?? _nameCondition.GetString(session, userdata);
            return session.GetFlag(flag) != inverted ? One : Zero;
        }
    }
}
