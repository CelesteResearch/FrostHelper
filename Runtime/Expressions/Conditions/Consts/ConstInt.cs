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

namespace Celeste.Mods.FrostHelper.Runtime.Expressions.Conditions.Consts
{
    using Color = Celeste.Core.Display.PackedColor;

    internal sealed class ConstInt(int x) : Condition, IConstCondition<int>, IConstCondition<float>
    {
        private readonly object _boxed = x;

        public override object Get(Session session, object? userdata) => _boxed;

        internal override void Emit(ConditionCompilationCtx ctx, Type targetType)
        {
            ctx.Il.EmitLoadConstAs(_boxed, targetType);
        }

        internal override bool UsesCurrentConditionLocalInEmit => false;

        public override bool OnlyChecksFlags() => true;

        public int Value => x;

        protected internal override Type ReturnType => typeof(int);

        protected override IEnumerable<object> GetArgsForDebugPrint() => [_boxed];

        float IConstCondition<float>.Value => Value;
    }
}
