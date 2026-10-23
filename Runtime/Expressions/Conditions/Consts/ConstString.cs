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

    internal sealed class ConstString(string x) : Condition, IConstCondition<string>
    {
        public string Value => x;

        public override object Get(Session session, object? userdata) => x;

        internal override void Emit(ConditionCompilationCtx ctx, Type targetType)
        {
            ctx.Il.EmitLoadConstAs(x, targetType);
        }

        internal override bool UsesCurrentConditionLocalInEmit => false;

        public override bool OnlyChecksFlags() => true;

        protected internal override Type ReturnType => typeof(string);

        protected override IEnumerable<object> GetArgsForDebugPrint() => [x];
    }
}
