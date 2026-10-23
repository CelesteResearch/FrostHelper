using Celeste.Mods.FrostHelper.Runtime.Expressions.Persistence;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;
using MonoMod.Utils;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Conditions;
using Session = Celeste.Simulation.Runtime.Session.ChapterCompletionSession;

namespace Celeste.Mods.FrostHelper.Runtime.Expressions.Compilation
{
    internal sealed class CompiledCondition<T> : ISavestatePersisted, IDisposable
    {
        private static readonly
            ConditionalWeakTable<FrostConditionRuntime, ConditionalWeakTable<Condition, CompiledCondition<T>>> Cache =
                new();

        private static int _compiledAmt;

        public static CompiledCondition<T> GetFor(Condition condition, FrostConditionRuntime runtime)
        {
            return Cache.GetValue(runtime, static r => new())
                .GetValue(condition, c => new CompiledCondition<T>(c, runtime));
        }

        private readonly FrostConditionRuntime runtime;

        private CompiledCondition(Condition basedOn, FrostConditionRuntime runtime)
        {
            SourceCondition = basedOn;
            this.runtime = runtime;
        }

        private Func<Session, object?, Condition, FrostConditionRuntime, T>? _compiled;

        private bool _attemptedToCompile;

        internal Condition SourceCondition { get; }

        internal DynamicMethodDefinition? CompiledMethod { get; private set; }

        internal Exception? CompilationException { get; private set; }

        public T Get(Session session, object? userdata)
        {
            if (!_attemptedToCompile)
            {
                _attemptedToCompile = true;
                try
                {
                    _compiled = Jit();
                }
                catch (Exception ex)
                {
                    CompilationException = ex;
                    runtime.LogCompilationFailure(
                        $"Failed to compile session expression '{SourceCondition.SourceText}', falling back to interpreter: {ex}");
                }
            }

            return _compiled is null
                ? SourceCondition.Get<T>(session, userdata)
                : _compiled(session, userdata, SourceCondition, runtime);
        }

        public TOther GetOther<TOther>(Session session, object? userdata)
        {
            var orig = Get(session, userdata);

            return Condition.Coerce<TOther>(orig!, runtime);
        }

        internal Func<Session, object?, Condition, FrostConditionRuntime, T>? Jit()
        {
            DynamicMethodDefinition method = new DynamicMethodDefinition(
                $"FrostHelper.<CompiledCondition.{typeof(T)}.{Interlocked.Increment(ref _compiledAmt)}>",
                typeof(T),
                [typeof(Session), typeof(object), typeof(Condition), typeof(FrostConditionRuntime)]);

            var il = method.GetILGenerator();

            var ctx = new ConditionCompilationCtx
            {
                CurrentCondition = il.DeclareLocal(typeof(Condition)),
                Il = il, Runtime = runtime,
            };

            if (SourceCondition.UsesCurrentConditionLocalInEmit)
            {
                il.Emit(OpCodes.Ldarg_2);
                il.Emit(OpCodes.Stloc, ctx.CurrentCondition);
            }

            SourceCondition.Emit(ctx, typeof(T));

            il.Emit(OpCodes.Ret);

            _compiled = method.Generate().CreateDelegate<Func<Session, object?, Condition, FrostConditionRuntime, T>>();
            CompiledMethod = method;

            return _compiled;
        }

        ~CompiledCondition()
        {
            Dispose();
        }

        public void Dispose()
        {
            _compiled = null;
            _attemptedToCompile = false;
            CompiledMethod?.Dispose();
            CompiledMethod = null;
            GC.SuppressFinalize(this);
        }
    }
}
