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


    internal sealed class CounterAccessorCondition(string name) : Condition
    {
        private Session.Counter? _valueCounter;
        private WeakReference<Session>? _lastSession;

        private static readonly MethodInfo MethodGetInt
            = typeof(CounterAccessorCondition).GetMethod(nameof(GetCached),
                BindingFlags.Instance | BindingFlags.NonPublic)!;

        private int GetCached(Session session)
        {
            if ((_lastSession?.TryGetTarget(out var last) ?? false) && last != session)
            {
                _valueCounter = null;
                _lastSession = null;
            }

            _lastSession ??= new WeakReference<Session>(session);
            _valueCounter ??= CounterObjectAccess.Get(session, name);

            return _valueCounter.Value;
        }

        public override object Get(Session session, object? userdata)
        {
            return GetCached(session);
        }

        internal override void Emit(ConditionCompilationCtx ctx, Type targetType)
        {
            ctx.EmitLoadCurrentCondition<CounterAccessorCondition>();
            ctx.EmitLoadSession();
            ctx.Il.Emit(OpCodes.Callvirt, MethodGetInt);
            ctx.EmitConvertTo(typeof(int), targetType);
        }

        protected internal override Type ReturnType => typeof(int);

        protected override IEnumerable<object> GetArgsForDebugPrint() => [name];
    }
}
