using Celeste.Mods.FrostHelper.Runtime.Expressions.Conditions.Consts;
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

namespace Celeste.Mods.FrostHelper.Runtime.Expressions.Commands
{
    internal sealed partial class InstanceFunctionCommands
    {
        internal interface IInstanceFunctionCommand
        {
            public Condition FieldCondition { get; }
        }

        internal interface IOneArgFunc<in TField, in TArg, out TResult>
        {
        }

        internal interface IOneArgSessionFunc<in TField, in TArg, out TResult>
        {
        }

        internal sealed class DynamicInstanceFunction(
            string functionName,
            Condition target,
            IReadOnlyList<Condition> arguments,
            IExpressionContext ctx) : Condition
        {
            public override object Get(Session session, object? userdata)
            {
                var obj = target.Get(session, userdata);
                if (_cache.TryGetValue(obj.GetType(), out var cached))
                    return cached.Get(session, userdata);

                var factory = ctx.Runtime.InstanceFunctionCommands.GetFactory(obj.GetType(), functionName, ctx);
                string? errorMessage = null;
                if (factory is null || !factory.Factory(target, arguments, out var condition, out errorMessage))
                {
                    ctx.Runtime.Notify(
                        $"Failed to create Session Expression function: '{functionName}', called on '{obj.GetType()}':\n{errorMessage ?? "function not found"}");
                    _cache[obj.GetType()] = new ConstInt(0);
                    return Zero;
                }

                condition.Runtime = ctx.Runtime;
                condition.Descriptor = factory.Descriptor;
                _cache[obj.GetType()] = condition;
                return condition.Get(session, userdata);
            }

            private readonly Dictionary<Type, Condition> _cache = [];
        }

        internal sealed class OneArgInstanceFunc<TField, TArg, TResult, TOp> : Condition, IInstanceFunctionCommand
            where TOp : IOneArgFunc<TField, TArg, TResult>
        {
            private readonly Condition _field;
            private readonly Condition _arg;

            private static readonly FieldInfo FieldField =
                typeof(OneArgInstanceFunc<TField, TArg, TResult, TOp>).GetField(nameof(_field),
                    BindingFlags.Instance | BindingFlags.NonPublic)!;

            private static readonly FieldInfo ArgField =
                typeof(OneArgInstanceFunc<TField, TArg, TResult, TOp>).GetField(nameof(_arg),
                    BindingFlags.Instance | BindingFlags.NonPublic)!;

            public static bool TryCreate(Condition field, IReadOnlyList<Condition> args,
                [NotNullWhen(true)] out Condition? result,
                [NotNullWhen(false)] out string? errorMessage)
            {
                result = null;
                errorMessage = null;

                if (args is not [{ } onlyArg])
                {
                    return FunctionCommands.FunctionCondition.ArgumentAmtMismatch(args.Count, 1, out result,
                        out errorMessage);
                }

                result = new OneArgInstanceFunc<TField, TArg, TResult, TOp>(field, onlyArg);
                return true;
            }

            public OneArgInstanceFunc(Condition field, Condition arg)
            {
                _arg = arg;
                _field = field;
            }

            public override object Get(Session session, object? userdata)
            {
                var field = _field.Get<TField>(session, userdata);
                var arg = _arg.Get<TArg>(session, userdata);

                return InstanceOperation<TField, TArg, TResult, TOp>.Invoke(field, arg, Runtime)!;
            }

            internal override void Emit(ConditionCompilationCtx ctx, Type targetType)
            {
                var il = ctx.Il;
                LocalBuilder? tempOrigCond = null;

                il.EmitSwapOutCurrentCondition(ref tempOrigCond, ctx, _field, FieldField);
                _field.Emit(ctx, typeof(TField));
                il.EmitSwapOutCurrentCondition(ref tempOrigCond, ctx, _arg, ArgField);
                _arg.Emit(ctx, typeof(TArg));

                il.EmitRevertCurrentCondition(tempOrigCond, ctx);

                if (!InstanceOperation<TField, TArg, TResult, TOp>.Emit(ctx, targetType))
                {
                    ctx.EmitLoadRuntime();
                    il.Emit(OpCodes.Call, typeof(TOp).GetMethod("Invoke")!);
                    ctx.EmitConvertTo(typeof(TResult), targetType);
                }
            }

            internal override bool UsesCurrentConditionLocalInEmit =>
                _field.UsesCurrentConditionLocalInEmit || _arg.UsesCurrentConditionLocalInEmit;

            protected internal override Type ReturnType => typeof(TResult);

            public Condition FieldCondition => _field;
        }

        internal sealed class OneArgSessionInstanceFunc<TField, TArg, TResult, TOp> : Condition,
            IInstanceFunctionCommand
            where TOp : IOneArgSessionFunc<TField, TArg, TResult>
        {
            private readonly Condition _field;
            private readonly Condition _arg;

            private static readonly FieldInfo FieldField =
                typeof(OneArgSessionInstanceFunc<TField, TArg, TResult, TOp>).GetField(nameof(_field),
                    BindingFlags.Instance | BindingFlags.NonPublic)!;

            private static readonly FieldInfo ArgField =
                typeof(OneArgSessionInstanceFunc<TField, TArg, TResult, TOp>).GetField(nameof(_arg),
                    BindingFlags.Instance | BindingFlags.NonPublic)!;

            public static bool TryCreate(Condition field, IReadOnlyList<Condition> args,
                [NotNullWhen(true)] out Condition? result,
                [NotNullWhen(false)] out string? errorMessage)
            {
                result = null;
                errorMessage = null;

                if (args is not [{ } onlyArg])
                {
                    return FunctionCommands.FunctionCondition.ArgumentAmtMismatch(args.Count, 1, out result,
                        out errorMessage);
                }

                result = new OneArgSessionInstanceFunc<TField, TArg, TResult, TOp>(field, onlyArg);
                return true;
            }

            public OneArgSessionInstanceFunc(Condition field, Condition arg)
            {
                _arg = arg;
                _field = field;

                InstanceSessionOperation<TField, TArg, TResult, TOp>.OnCreated(field, arg);
            }

            public override object Get(Session session, object? userdata)
            {
                var field = _field.Get<TField>(session, userdata);
                var arg = _arg.Get<TArg>(session, userdata);

                return InstanceSessionOperation<TField, TArg, TResult, TOp>.Invoke(session, userdata, field, arg)!;
            }

            internal override void Emit(ConditionCompilationCtx ctx, Type targetType)
            {
                var il = ctx.Il;
                LocalBuilder? tempOrigCond = null;

                if (InstanceSessionOperation<TField, TArg, TResult, TOp>.UsesStack)
                {
                    ctx.EmitLoadSession();
                    ctx.EmitLoadUserdata();
                }

                il.EmitSwapOutCurrentCondition(ref tempOrigCond, ctx, _field, FieldField);
                _field.Emit(ctx, typeof(TField));
                il.EmitSwapOutCurrentCondition(ref tempOrigCond, ctx, _arg, ArgField);
                _arg.Emit(ctx, typeof(TArg));

                il.EmitRevertCurrentCondition(tempOrigCond, ctx);

                if (!InstanceSessionOperation<TField, TArg, TResult, TOp>.Emit(ctx, targetType, _field, _arg))
                {
                    il.Emit(OpCodes.Call, typeof(TOp).GetMethod("Invoke")!);
                    ctx.EmitConvertTo(typeof(TResult), targetType);
                }
            }

            internal override bool UsesCurrentConditionLocalInEmit =>
                _field.UsesCurrentConditionLocalInEmit || _arg.UsesCurrentConditionLocalInEmit;

            protected internal override Type ReturnType => typeof(TResult);

            public Condition FieldCondition => _field;
        }

        internal struct StringIsMatch : IOneArgFunc<string, string, int>
        {
            public static int Invoke(string field, string arg, FrostConditionRuntime runtime)
            {
                try
                {
                    return Regex.IsMatch(field, arg, RegexOptions.Compiled) ? 1 : 0;
                }
                catch (ArgumentException ex) when (ex is not ArgumentNullException)
                {
                    if (runtime.InstanceFunctionCommands.NotifiedInvalidRegexes.Add(arg))
                        runtime.Notify($"Invalid regex: {arg}: {ex.Message}");
                    return 0;
                }
            }


            public static string ArgName => "regex";

            public static bool Emit(ConditionCompilationCtx ctx, Type targetType) => false;
        }

        internal struct Str : IOneArgFunc<object, string, string>
        {
            public static string Invoke(object field, string arg, FrostConditionRuntime runtime)
            {
                if (field is IFormattable formattable)
                {
                    try
                    {
                        return formattable.ToString(arg, CultureInfo.InvariantCulture);
                    }
                    catch (FormatException)
                    {
                        runtime.Notify($"Invalid format string as 'str()' argument: {arg}");
                    }
                }

                return field.ToString() ?? "";
            }

            public static string ArgName => "format";

            public static bool Emit(ConditionCompilationCtx ctx, Type targetType) => false;
        }

        private static class EnumerableEmitUtils
        {
            /// <summary>
            /// Exprects [ field(IEnumerable), arg(LambdaCondition) ] on the stack
            /// </summary>
            /// <param name="ctx"></param>
            /// <param name="fieldCondition"></param>
            /// <param name="argCondition">Should be a lambda definition</param>
            /// <param name="emitPostLambdaCallCode">(endLoopLabel)</param>
            public static void EmitEnumerateThroughFieldAndCallArg<TLambdaReturnType>(ConditionCompilationCtx ctx,
                Condition fieldCondition,
                Condition argCondition, Action<Label> emitPostLambdaCallCode)
            {
                var il = ctx.Il;
                LocalBuilder? tempCurrentCond = null;
                if (argCondition is not LambdaDefinitionCondition lambdaDefinitionCondition)
                {
                    throw new Exception("Expected argument to IEnumerable fold to be a lambda.");
                }

                var lambda = lambdaDefinitionCondition.Instance;
                var lambdaLocal = il.DeclareLocal(typeof(LambdaCondition));
                il.Emit(OpCodes.Stloc, lambdaLocal);

                // field(IEnumerable) is now top of stack
                var enumerableType = fieldCondition.ReturnType ?? typeof(IEnumerable);
                var getEnumerator = enumerableType.GetMethod(nameof(IEnumerable.GetEnumerator));
                if (getEnumerator is null)
                    throw new Exception("Enumerable type doesn't have GetEnumerator???");

                var enumeratorLocal = il.DeclareLocal(getEnumerator.ReturnType);
                il.Emit(OpCodes.Callvirt, getEnumerator);
                il.Emit(OpCodes.Stloc, enumeratorLocal);

                var nextElementLabel = il.DefineLabel();
                var endLoopLabel = il.DefineLabel();

                il.MarkLabel(nextElementLabel);

                il.EmitLdlocOrLdloca(enumeratorLocal);
                il.Emit(OpCodes.Callvirt, getEnumerator.ReturnType.GetMethod(nameof(IEnumerator.MoveNext))
                                          ?? typeof(IEnumerator).GetMethod(nameof(IEnumerator.MoveNext))!);
                il.Emit(OpCodes.Brfalse, endLoopLabel);

                il.Emit(OpCodes.Ldloc, lambdaLocal);
                il.Emit(OpCodes.Ldc_I4_0);
                il.EmitLdlocOrLdloca(enumeratorLocal);
                var currentProp = getEnumerator.ReturnType.GetProperty(nameof(IEnumerator.Current))!.GetMethod!;
                lambdaDefinitionCondition.ArgumentTypes[0] = currentProp.ReturnType;
                il.Emit(OpCodes.Callvirt, currentProp);
                ctx.EmitConvertTo(currentProp.ReturnType, typeof(object));
                lambda.EmitSetArgument(ctx);

                ctx.EmitSwapOutCurrentCondition(ref tempCurrentCond, lambda,
                    () => { il.Emit(OpCodes.Ldloc, lambdaLocal); });
                lambda.Emit(ctx, typeof(TLambdaReturnType));

                emitPostLambdaCallCode(endLoopLabel);
                il.Emit(OpCodes.Br, nextElementLabel);

                il.MarkLabel(endLoopLabel);
            }
        }

        internal struct EnumerableSum : IOneArgSessionFunc<IEnumerable, LambdaCondition, float>
        {
            public static float Invoke(Session session, object? userdata, IEnumerable field, LambdaCondition callback)
            {
                float sum = 0;
                foreach (var obj in field)
                {
                    callback.SetArgument(0, obj);
                    sum += callback.GetFloat(session, userdata);
                }

                return sum;
            }

            public static string ArgName => "callback";

            public static bool EmitCodeUsesSessionAndUserdataOnStack => false;

            public static bool Emit(ConditionCompilationCtx ctx, Type targetType, Condition fieldCondition,
                Condition argCondition)
            {
                var il = ctx.Il;
                var sumLocal = il.DeclareLocal(typeof(float));
                il.Emit(OpCodes.Ldc_R4, 0f);
                il.Emit(OpCodes.Stloc, sumLocal);

                EnumerableEmitUtils.EmitEnumerateThroughFieldAndCallArg<float>(ctx, fieldCondition, argCondition,
                    emitPostLambdaCallCode: (endLoopLabel) =>
                    {
                        il.Emit(OpCodes.Ldloc, sumLocal);
                        il.Emit(OpCodes.Add);
                        il.Emit(OpCodes.Stloc, sumLocal);
                    });

                il.Emit(OpCodes.Ldloc, sumLocal);
                ctx.EmitConvertTo(typeof(float), targetType);
                return true;
            }

            public static void OnCreated(Condition fieldCondition, Condition argCondition)
            {
            }
        }

        internal struct EnumerableAll : IOneArgSessionFunc<IEnumerable, LambdaCondition, int>
        {
            public static int Invoke(Session session, object? userdata, IEnumerable field, LambdaCondition callback)
            {
                foreach (var obj in field)
                {
                    callback.SetArgument(0, obj);
                    if (!callback.Check(session, userdata))
                        return 0;
                }

                return 1;
            }

            public static string ArgName => "predicate";

            public static bool EmitCodeUsesSessionAndUserdataOnStack => false;

            public static bool Emit(ConditionCompilationCtx ctx, Type targetType, Condition fieldCondition,
                Condition argCondition)
            {
                var il = ctx.Il;
                var retLocal = il.DeclareLocal(typeof(bool));
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Stloc, retLocal);

                EnumerableEmitUtils.EmitEnumerateThroughFieldAndCallArg<float>(ctx, fieldCondition, argCondition,
                    emitPostLambdaCallCode: (endLoopLabel) =>
                    {
                        il.Emit(OpCodes.Dup);
                        il.Emit(OpCodes.Stloc, retLocal);
                        il.Emit(OpCodes.Brfalse, endLoopLabel);
                    });

                il.Emit(OpCodes.Ldloc, retLocal);
                ctx.EmitConvertTo(typeof(bool), targetType);
                return true;
            }

            public static void OnCreated(Condition fieldCondition, Condition argCondition)
            {
            }
        }

        internal struct EnumerableAny : IOneArgSessionFunc<IEnumerable, LambdaCondition, int>
        {
            public static int Invoke(Session session, object? userdata, IEnumerable field, LambdaCondition callback)
            {
                foreach (var obj in field)
                {
                    callback.SetArgument(0, obj);
                    if (callback.Check(session, userdata))
                        return 1;
                }

                return 0;
            }

            public static string ArgName => "predicate";

            public static bool EmitCodeUsesSessionAndUserdataOnStack => false;

            public static bool Emit(ConditionCompilationCtx ctx, Type targetType, Condition fieldCondition,
                Condition argCondition)
            {
                var il = ctx.Il;
                var retLocal = il.DeclareLocal(typeof(bool));
                il.Emit(OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Stloc, retLocal);

                EnumerableEmitUtils.EmitEnumerateThroughFieldAndCallArg<float>(ctx, fieldCondition, argCondition,
                    emitPostLambdaCallCode: (endLoopLabel) =>
                    {
                        il.Emit(OpCodes.Dup);
                        il.Emit(OpCodes.Stloc, retLocal);
                        il.Emit(OpCodes.Brtrue, endLoopLabel);
                    });

                il.Emit(OpCodes.Ldloc, retLocal);
                ctx.EmitConvertTo(typeof(bool), targetType);
                return true;
            }

            public static void OnCreated(Condition fieldCondition, Condition argCondition)
            {
            }
        }
    }
}
