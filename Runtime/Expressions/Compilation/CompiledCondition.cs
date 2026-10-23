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
    internal static class CompiledCondition
    {
        private static readonly Dictionary<Type, (MethodInfo GetForMethod, Type DelegateType, MethodInfo GetMethod)>
            Cache = [];

        /// <summary>
        /// Returns a Func{Session, object, returnType} that invokes a compiled session expression.
        /// </summary>
        public static Delegate GetDelegateFor(Condition condition, Type returnType, FrostConditionRuntime runtime)
        {
            lock (Cache)
            {
                if (!Cache.TryGetValue(returnType, out var result))
                {
                    var t = typeof(CompiledCondition<>).MakeGenericType(returnType);
                    result.GetForMethod = t.GetMethod(nameof(CompiledCondition<>.GetFor))!;
                    result.DelegateType = typeof(Func<,,>).MakeGenericType(typeof(Session), typeof(object), returnType);
                    result.GetMethod = t.GetMethod(nameof(CompiledCondition<>.Get))!;
                    Cache[returnType] = result;
                }

                var compiledCondition = result.GetForMethod.Invoke(null, [condition, runtime]);

                return Delegate.CreateDelegate(result.DelegateType, compiledCondition, result.GetMethod);
            }
        }
    }
}
