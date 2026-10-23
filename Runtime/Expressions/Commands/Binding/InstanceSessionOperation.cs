using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using System.Globalization;
using System.Diagnostics.CodeAnalysis;
using Celeste.Mods.FrostHelper.Runtime.Expressions;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Conditions;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Compilation;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Descriptors;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Contexts;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Commands.Binding;
using Session = Celeste.Simulation.Runtime.Session.ChapterCompletionSession;

namespace Celeste.Mods.FrostHelper.Runtime.Expressions.Commands.Binding
{
    internal static class InstanceSessionOperation<TField, TArg, TResult, TOp>
    {
        internal static readonly Func<Session, object?, TField, TArg, TResult> Invoke =
            (Func<Session, object?, TField, TArg, TResult>)typeof(TOp).GetMethod("Invoke")!.CreateDelegate(
                typeof(Func<Session, object?, TField, TArg, TResult>));

        internal static readonly Func<ConditionCompilationCtx, Type, Condition, Condition, bool> Emit =
            (Func<ConditionCompilationCtx, Type, Condition, Condition, bool>)typeof(TOp).GetMethod("Emit")!
                .CreateDelegate(typeof(Func<ConditionCompilationCtx, Type, Condition, Condition, bool>));

        internal static readonly Action<Condition, Condition> OnCreated =
            (Action<Condition, Condition>)typeof(TOp).GetMethod("OnCreated")!.CreateDelegate(
                typeof(Action<Condition, Condition>));

        internal static readonly bool UsesStack =
            (bool)typeof(TOp).GetProperty("EmitCodeUsesSessionAndUserdataOnStack")!.GetValue(null)!;
    }
}
