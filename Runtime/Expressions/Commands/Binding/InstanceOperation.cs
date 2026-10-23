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
    internal static class InstanceOperation<TField, TArg, TResult, TOp>
    {
        internal static readonly Func<TField, TArg, FrostConditionRuntime, TResult> Invoke =
            (Func<TField, TArg, FrostConditionRuntime, TResult>)typeof(TOp).GetMethod("Invoke")!.CreateDelegate(
                typeof(Func<TField, TArg, FrostConditionRuntime, TResult>));

        internal static readonly Func<ConditionCompilationCtx, Type, bool> Emit =
            (Func<ConditionCompilationCtx, Type, bool>)typeof(TOp).GetMethod("Emit")!.CreateDelegate(
                typeof(Func<ConditionCompilationCtx, Type, bool>));
    }
}
