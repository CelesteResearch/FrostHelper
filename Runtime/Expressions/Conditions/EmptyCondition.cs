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


    internal sealed class EmptyCondition : Condition
    {
        internal static readonly EmptyCondition Instance = new();

        public override object Get(Session session, object? userdata)
        {
            return One;
        }

        internal override void Emit(ConditionCompilationCtx ctx, Type targetType)
        {
            ctx.Il.EmitLoadConstAs(One, targetType);
        }

        internal override bool UsesCurrentConditionLocalInEmit => false;

        public override bool OnlyChecksFlags() => true;
    }
}
