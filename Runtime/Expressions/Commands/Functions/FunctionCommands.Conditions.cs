using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;
using Celeste.Mods.FrostHelper.Runtime.Expressions;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Conditions;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Compilation;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Descriptors;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Contexts;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Commands.Binding;
using Session = Celeste.Simulation.Runtime.Session.ChapterCompletionSession;
using PackedColor = Celeste.Core.Display.PackedColor;

namespace Celeste.Mods.FrostHelper.Runtime.Expressions.Commands
{
    internal sealed partial class FunctionCommands
    {
        private sealed class PureMathCondition<TArg, TRet, TOp>(Condition x) : FunctionCondition(x)
            where TOp : struct, IPureFunc<TArg, TRet>
        {
            private readonly Condition _innerCondition = x;

            private static readonly FieldInfo InnerConditionFieldInfo
                = typeof(PureMathCondition<TArg, TRet, TOp>).GetField(nameof(_innerCondition),
                    BindingFlags.Instance | BindingFlags.NonPublic)!;

            public override object Get(Session session, object? userdata)
            {
                return OneArgOperation<TArg, TRet, TOp>.Invoke(_innerCondition.Get<TArg>(session, userdata))!;
            }

            internal override void Emit(ConditionCompilationCtx ctx, Type targetType)
            {
                var il = ctx.Il;
                LocalBuilder? tempOrigCond = null;

                il.EmitSwapOutCurrentCondition(ref tempOrigCond, ctx, _innerCondition, InnerConditionFieldInfo);

                _innerCondition.Emit(ctx, typeof(TArg));

                il.EmitRevertCurrentCondition(tempOrigCond, ctx);

                il.Emit(OpCodes.Call, OperationMetadata<TOp>.GetMethod);
                il.EmitConvertToInSessionExpression(typeof(TRet), targetType);
            }

            internal override bool UsesCurrentConditionLocalInEmit => _innerCondition.UsesCurrentConditionLocalInEmit;

            protected internal override Type ReturnType => typeof(TRet);
        }

        private sealed class Session1ArgCondition<TArg, TRet, TOp>(Condition x) : FunctionCondition(x)
            where TOp : struct, ISessionFunc<TArg, TRet>
        {
            private readonly Condition _innerCondition = x;

            private static readonly FieldInfo InnerConditionFieldInfo
                = typeof(Session1ArgCondition<TArg, TRet, TOp>).GetField(nameof(_innerCondition),
                    BindingFlags.Instance | BindingFlags.NonPublic)!;

            public override object Get(Session session, object? userdata)
            {
                return SessionOperation<TArg, TRet, TOp>.Invoke(session, _innerCondition.Get<TArg>(session, userdata),
                    Runtime)!;
            }

            internal override void Emit(ConditionCompilationCtx ctx, Type targetType)
            {
                var il = ctx.Il;
                LocalBuilder? tempOrigCond = null;

                ctx.EmitLoadSession();

                il.EmitSwapOutCurrentCondition(ref tempOrigCond, ctx, _innerCondition, InnerConditionFieldInfo);
                _innerCondition.Emit(ctx, typeof(TArg));

                il.EmitRevertCurrentCondition(tempOrigCond, ctx);

                ctx.EmitLoadRuntime();
                il.Emit(OpCodes.Call, OperationMetadata<TOp>.GetMethod);
                il.EmitConvertToInSessionExpression(typeof(TRet), targetType);
            }

            internal override bool UsesCurrentConditionLocalInEmit => _innerCondition.UsesCurrentConditionLocalInEmit;

            protected internal override Type ReturnType => typeof(TRet);
        }

        private sealed class PureMathTwoArgCondition<TArg1, TArg2, TRet, TOp>(Condition x, Condition y)
            : FunctionCondition(x)
            where TOp : struct, IPureFunc<TArg1, TArg2, TRet>
        {
            private readonly Condition _x = x;
            private readonly Condition _y = y;

            private static readonly FieldInfo XFieldInfo
                = typeof(PureMathTwoArgCondition<TArg1, TArg2, TRet, TOp>).GetField(nameof(_x),
                    BindingFlags.Instance | BindingFlags.NonPublic)!;

            private static readonly FieldInfo YFieldInfo
                = typeof(PureMathTwoArgCondition<TArg1, TArg2, TRet, TOp>).GetField(nameof(_y),
                    BindingFlags.Instance | BindingFlags.NonPublic)!;

            public override object Get(Session session, object? userdata)
            {
                return TwoArgOperation<TArg1, TArg2, TRet, TOp>.Invoke(_x.Get<TArg1>(session, userdata),
                    _y.Get<TArg2>(session, userdata))!;
            }

            internal override void Emit(ConditionCompilationCtx ctx, Type targetType)
            {
                var il = ctx.Il;
                LocalBuilder? tempOrigCond = null;

                il.EmitSwapOutCurrentCondition(ref tempOrigCond, ctx, _x, XFieldInfo);
                _x.Emit(ctx, typeof(TArg1));
                il.EmitSwapOutCurrentCondition(ref tempOrigCond, ctx, _y, YFieldInfo);
                _y.Emit(ctx, typeof(TArg2));

                il.EmitRevertCurrentCondition(tempOrigCond, ctx);

                il.Emit(OpCodes.Call, OperationMetadata<TOp>.GetMethod);
                il.EmitConvertToInSessionExpression(typeof(TRet), targetType);
            }

            internal override bool UsesCurrentConditionLocalInEmit =>
                _x.UsesCurrentConditionLocalInEmit || _y.UsesCurrentConditionLocalInEmit;

            protected internal override Type ReturnType => typeof(TRet);
        }

        private sealed class PureMathThreeArgCondition<TArg1, TArg2, TArg3, TRet, TOp>(
            Condition x,
            Condition y,
            Condition z) : FunctionCondition(x)
            where TOp : struct, IPureFunc<TArg1, TArg2, TArg3, TRet>
        {
            private readonly Condition _x = x;
            private readonly Condition _y = y;
            private readonly Condition _z = z;

            private static readonly FieldInfo XFieldInfo
                = typeof(PureMathThreeArgCondition<TArg1, TArg2, TArg3, TRet, TOp>).GetField(nameof(_x),
                    BindingFlags.Instance | BindingFlags.NonPublic)!;

            private static readonly FieldInfo YFieldInfo
                = typeof(PureMathThreeArgCondition<TArg1, TArg2, TArg3, TRet, TOp>).GetField(nameof(_y),
                    BindingFlags.Instance | BindingFlags.NonPublic)!;

            private static readonly FieldInfo ZFieldInfo
                = typeof(PureMathThreeArgCondition<TArg1, TArg2, TArg3, TRet, TOp>).GetField(nameof(_z),
                    BindingFlags.Instance | BindingFlags.NonPublic)!;

            public override object Get(Session session, object? userdata)
            {
                return ThreeArgOperation<TArg1, TArg2, TArg3, TRet, TOp>.Invoke(
                    _x.Get<TArg1>(session, userdata),
                    _y.Get<TArg2>(session, userdata),
                    _z.Get<TArg3>(session, userdata))!;
            }

            internal override void Emit(ConditionCompilationCtx ctx, Type targetType)
            {
                var il = ctx.Il;
                LocalBuilder? tempOrigCond = null;

                il.EmitSwapOutCurrentCondition(ref tempOrigCond, ctx, _x, XFieldInfo);
                _x.Emit(ctx, typeof(TArg1));
                il.EmitSwapOutCurrentCondition(ref tempOrigCond, ctx, _y, YFieldInfo);
                _y.Emit(ctx, typeof(TArg2));
                il.EmitSwapOutCurrentCondition(ref tempOrigCond, ctx, _z, ZFieldInfo);
                _z.Emit(ctx, typeof(TArg3));

                il.EmitRevertCurrentCondition(tempOrigCond, ctx);

                il.Emit(OpCodes.Call, OperationMetadata<TOp>.GetMethod);
                il.EmitConvertToInSessionExpression(typeof(TRet), targetType);
            }

            internal override bool UsesCurrentConditionLocalInEmit => _x.UsesCurrentConditionLocalInEmit
                                                                      || _y.UsesCurrentConditionLocalInEmit
                                                                      || _z.UsesCurrentConditionLocalInEmit;

            protected internal override Type ReturnType => typeof(TRet);
        }

        private sealed class PureMathFourArgCondition<TArg1, TArg2, TArg3, TArg4, TRet, TOp>(
            Condition x,
            Condition y,
            Condition z,
            Condition a) : FunctionCondition(x)
            where TOp : struct, IPureFunc<TArg1, TArg2, TArg3, TArg4, TRet>
        {
            private readonly Condition _x = x;
            private readonly Condition _y = y;
            private readonly Condition _z = z;
            private readonly Condition _a = a;

            private static readonly FieldInfo XFieldInfo
                = typeof(PureMathFourArgCondition<TArg1, TArg2, TArg3, TArg4, TRet, TOp>).GetField(nameof(_x),
                    BindingFlags.Instance | BindingFlags.NonPublic)!;

            private static readonly FieldInfo YFieldInfo
                = typeof(PureMathFourArgCondition<TArg1, TArg2, TArg3, TArg4, TRet, TOp>).GetField(nameof(_y),
                    BindingFlags.Instance | BindingFlags.NonPublic)!;

            private static readonly FieldInfo ZFieldInfo
                = typeof(PureMathFourArgCondition<TArg1, TArg2, TArg3, TArg4, TRet, TOp>).GetField(nameof(_z),
                    BindingFlags.Instance | BindingFlags.NonPublic)!;

            private static readonly FieldInfo AFieldInfo
                = typeof(PureMathFourArgCondition<TArg1, TArg2, TArg3, TArg4, TRet, TOp>).GetField(nameof(_a),
                    BindingFlags.Instance | BindingFlags.NonPublic)!;

            public override object Get(Session session, object? userdata)
            {
                return FourArgOperation<TArg1, TArg2, TArg3, TArg4, TRet, TOp>.Invoke(
                    _x.Get<TArg1>(session, userdata),
                    _y.Get<TArg2>(session, userdata),
                    _z.Get<TArg3>(session, userdata),
                    _a.Get<TArg4>(session, userdata))!;
            }

            internal override void Emit(ConditionCompilationCtx ctx, Type targetType)
            {
                var il = ctx.Il;
                LocalBuilder? tempOrigCond = null;

                il.EmitSwapOutCurrentCondition(ref tempOrigCond, ctx, _x, XFieldInfo);
                _x.Emit(ctx, typeof(TArg1));
                il.EmitSwapOutCurrentCondition(ref tempOrigCond, ctx, _y, YFieldInfo);
                _y.Emit(ctx, typeof(TArg2));
                il.EmitSwapOutCurrentCondition(ref tempOrigCond, ctx, _z, ZFieldInfo);
                _z.Emit(ctx, typeof(TArg3));
                il.EmitSwapOutCurrentCondition(ref tempOrigCond, ctx, _a, AFieldInfo);
                _a.Emit(ctx, typeof(TArg4));

                il.EmitRevertCurrentCondition(tempOrigCond, ctx);

                il.Emit(OpCodes.Call, OperationMetadata<TOp>.GetMethod);
                il.EmitConvertToInSessionExpression(typeof(TRet), targetType);
            }

            internal override bool UsesCurrentConditionLocalInEmit => _x.UsesCurrentConditionLocalInEmit
                                                                      || _y.UsesCurrentConditionLocalInEmit
                                                                      || _z.UsesCurrentConditionLocalInEmit
                                                                      || _a.UsesCurrentConditionLocalInEmit;

            protected internal override Type ReturnType => typeof(TRet);
        }

        private static class PureMathCondition
        {
            public static bool TryCreateIntOrFloat<TInt, TFloat>(IReadOnlyList<Condition> args,
                [NotNullWhen(true)] out Condition? condition,
                [NotNullWhen(false)] out string? errorMessage)
                where TInt : struct, IPureMathFunc<int>
                where TFloat : struct, IPureMathFunc<float>
            {
                if (args is not [var only])
                {
                    return FunctionCondition.ArgumentAmtMismatch(args.Count, 1, out condition, out errorMessage);
                }

                if (only.ReturnType == typeof(int))
                {
                    return FunctionCondition.Ok(new PureMathCondition<int, int, TInt>(only), out condition,
                        out errorMessage);
                }

                return FunctionCondition.Ok(new PureMathCondition<float, float, TFloat>(only), out condition,
                    out errorMessage);
            }

            public static bool TryCreateFloat<TFloat>(IReadOnlyList<Condition> args,
                [NotNullWhen(true)] out Condition? condition,
                [NotNullWhen(false)] out string? errorMessage)
                where TFloat : struct, IPureMathFunc<float>
            {
                if (args is not [var only])
                {
                    return FunctionCondition.ArgumentAmtMismatch(args.Count, 1, out condition, out errorMessage);
                }

                return FunctionCondition.Ok(new PureMathCondition<float, float, TFloat>(only), out condition,
                    out errorMessage);
            }

            public static bool TryCreateTwoArgFloat<TFloat>(IReadOnlyList<Condition> args,
                [NotNullWhen(true)] out Condition? condition,
                [NotNullWhen(false)] out string? errorMessage)
                where TFloat : struct, IPureTwoArgMathFunc<float>
            {
                if (args is not [var left, var right])
                {
                    return FunctionCondition.ArgumentAmtMismatch(args.Count, 2, out condition, out errorMessage);
                }

                return FunctionCondition.Ok(new PureMathTwoArgCondition<float, float, float, TFloat>(left, right),
                    out condition, out errorMessage);
            }

            public static bool TryCreateTwoArg<TArg1, TArg2, TRet, TFunc>(IReadOnlyList<Condition> args,
                [NotNullWhen(true)] out Condition? condition,
                [NotNullWhen(false)] out string? errorMessage)
                where TFunc : struct, IPureFunc<TArg1, TArg2, TRet>
            {
                if (args is not [var left, var right])
                {
                    return FunctionCondition.ArgumentAmtMismatch(args.Count, 2, out condition, out errorMessage);
                }

                return FunctionCondition.Ok(new PureMathTwoArgCondition<TArg1, TArg2, TRet, TFunc>(left, right),
                    out condition, out errorMessage);
            }

            public static bool TryCreate<TArg1, TRet, TFunc>(IReadOnlyList<Condition> args,
                [NotNullWhen(true)] out Condition? condition,
                [NotNullWhen(false)] out string? errorMessage)
                where TFunc : struct, IPureFunc<TArg1, TRet>
            {
                if (args is not [var only])
                {
                    return FunctionCondition.ArgumentAmtMismatch(args.Count, 1, out condition, out errorMessage);
                }

                return FunctionCondition.Ok(new PureMathCondition<TArg1, TRet, TFunc>(only), out condition,
                    out errorMessage);
            }

            public static bool TryCreateSession<TArg1, TRet, TFunc>(IReadOnlyList<Condition> args,
                [NotNullWhen(true)] out Condition? condition,
                [NotNullWhen(false)] out string? errorMessage)
                where TFunc : struct, ISessionFunc<TArg1, TRet>
            {
                if (args is not [var only])
                {
                    return FunctionCondition.ArgumentAmtMismatch(args.Count, 1, out condition, out errorMessage);
                }

                return FunctionCondition.Ok(new Session1ArgCondition<TArg1, TRet, TFunc>(only), out condition,
                    out errorMessage);
            }

            public static bool TryCreatThreeArgFloat<TFloat>(IReadOnlyList<Condition> args,
                [NotNullWhen(true)] out Condition? condition,
                [NotNullWhen(false)] out string? errorMessage)
                where TFloat : struct, IPureThreeArgMathFunc<float>
            {
                if (args is not [var a, var b, var c])
                {
                    return FunctionCondition.ArgumentAmtMismatch(args.Count, 3, out condition, out errorMessage);
                }

                return FunctionCondition.Ok(new PureMathThreeArgCondition<float, float, float, float, TFloat>(a, b, c),
                    out condition, out errorMessage);
            }

            public static bool TryCreate<TArg1, TArg2, TRet, TOp>(IReadOnlyList<Condition> args,
                [NotNullWhen(true)] out Condition? condition,
                [NotNullWhen(false)] out string? errorMessage)
                where TOp : struct, IPureFunc<TArg1, TArg2, TRet>
            {
                if (args is not [var a, var b])
                {
                    return FunctionCondition.ArgumentAmtMismatch(args.Count, 3, out condition, out errorMessage);
                }

                return FunctionCondition.Ok(new PureMathTwoArgCondition<TArg1, TArg2, TRet, TOp>(a, b), out condition,
                    out errorMessage);
            }

            public static bool TryCreate<TArg1, TArg2, TArg3, TRet, TOp>(IReadOnlyList<Condition> args,
                [NotNullWhen(true)] out Condition? condition,
                [NotNullWhen(false)] out string? errorMessage)
                where TOp : struct, IPureFunc<TArg1, TArg2, TArg3, TRet>
            {
                if (args is not [var a, var b, var c])
                {
                    return FunctionCondition.ArgumentAmtMismatch(args.Count, 3, out condition, out errorMessage);
                }

                return FunctionCondition.Ok(new PureMathThreeArgCondition<TArg1, TArg2, TArg3, TRet, TOp>(a, b, c),
                    out condition, out errorMessage);
            }

            public static bool TryCreate<TArg1, TArg2, TArg3, TArg4, TRet, TOp>(IReadOnlyList<Condition> args,
                [NotNullWhen(true)] out Condition? condition,
                [NotNullWhen(false)] out string? errorMessage)
                where TOp : struct, IPureFunc<TArg1, TArg2, TArg3, TArg4, TRet>
            {
                if (args is not [var a, var b, var c, var d])
                {
                    return FunctionCondition.ArgumentAmtMismatch(args.Count, 4, out condition, out errorMessage);
                }

                return FunctionCondition.Ok(
                    new PureMathFourArgCondition<TArg1, TArg2, TArg3, TArg4, TRet, TOp>(a, b, c, d), out condition,
                    out errorMessage);
            }
        }

        private sealed class MinCondition(IEnumerable<Condition> x, Type type) : FunctionCondition(x)
        {
            private int GetIntImpl(Session session, object? userdata)
            {
                int min = int.MaxValue;
                foreach (var c in Conditions) min = Math.Min(c.GetNumber<int>(session, userdata), min);
                return min;
            }

            private float GetFloatImpl(Session session, object? userdata)
            {
                float min = float.MaxValue;
                foreach (var c in Conditions) min = MathF.Min(c.GetNumber<float>(session, userdata), min);
                return min;
            }

            protected internal override Type ReturnType => type;

            public override object Get(Session session, object? userdata)
            {
                if (ReturnType == typeof(int))
                    return GetIntImpl(session, userdata);
                return GetFloatImpl(session, userdata);
            }

            public static bool TryCreate(IReadOnlyList<Condition> args, [NotNullWhen(true)] out Condition? condition,
                [NotNullWhen(false)] out string? errorMessage)
            {
                if (args is not [_, ..])
                {
                    return TooFewArgs(args.Count, 1, out condition, out errorMessage);
                }

                if (args.All(x => x.ReturnType == typeof(int)))
                {
                    return Ok(new MinCondition(args, typeof(int)), out condition, out errorMessage);
                }

                return Ok(new MinCondition(args, typeof(float)), out condition, out errorMessage);
            }
        }

        private sealed class MaxCondition(IEnumerable<Condition> x, Type type) : FunctionCondition(x)
        {
            private int GetIntImpl(Session session, object? userdata)
            {
                int max = int.MinValue;
                foreach (var c in Conditions) max = Math.Max(c.GetNumber<int>(session, userdata), max);
                return max;
            }

            private float GetFloatImpl(Session session, object? userdata)
            {
                float max = float.MinValue;
                foreach (var c in Conditions) max = MathF.Max(c.GetNumber<float>(session, userdata), max);
                return max;
            }

            protected internal override Type ReturnType => type;

            public override object Get(Session session, object? userdata)
            {
                if (ReturnType == typeof(int))
                    return GetIntImpl(session, userdata);
                return GetFloatImpl(session, userdata);
            }

            public static bool TryCreate(IReadOnlyList<Condition> args, [NotNullWhen(true)] out Condition? condition,
                [NotNullWhen(false)] out string? errorMessage)
            {
                if (args is not [_, ..])
                {
                    return TooFewArgs(args.Count, 1, out condition, out errorMessage);
                }

                if (args.All(x => x.ReturnType == typeof(int)))
                {
                    return Ok(new MaxCondition(args, typeof(int)), out condition, out errorMessage);
                }

                return Ok(new MaxCondition(args, typeof(float)), out condition, out errorMessage);
            }
        }

        private sealed class VecCondition(Condition x, Condition y) : FunctionCondition(x, y)
        {
            private readonly Condition _x = x;
            private readonly Condition _y = y;

            private static readonly FieldInfo XFieldInfo
                = typeof(VecCondition).GetField(nameof(_x),
                    BindingFlags.Instance | BindingFlags.NonPublic)!;

            private static readonly FieldInfo YFieldInfo
                = typeof(VecCondition).GetField(nameof(_y),
                    BindingFlags.Instance | BindingFlags.NonPublic)!;

            protected internal override Type ReturnType => typeof(Vector2);

            public override object Get(Session session, object? userdata)
            {
                return new Vector2(_x.GetFloat(session, userdata), _y.GetFloat(session, userdata));
            }

            internal override void Emit(ConditionCompilationCtx ctx, Type targetType)
            {
                var il = ctx.Il;
                LocalBuilder? tempOrigCond = null;

                il.EmitSwapOutCurrentCondition(ref tempOrigCond, ctx, _x, XFieldInfo);
                _x.Emit(ctx, typeof(float));
                il.EmitSwapOutCurrentCondition(ref tempOrigCond, ctx, _y, YFieldInfo);
                _y.Emit(ctx, typeof(float));

                il.EmitRevertCurrentCondition(tempOrigCond, ctx);

                il.Emit(OpCodes.Newobj, typeof(Vector2).GetConstructor([typeof(float), typeof(float)])!);
                il.EmitConvertToInSessionExpression(typeof(Vector2), targetType);
            }

            internal override bool UsesCurrentConditionLocalInEmit =>
                _x.UsesCurrentConditionLocalInEmit || _y.UsesCurrentConditionLocalInEmit;

            public static bool TryCreate(IReadOnlyList<Condition> args, [NotNullWhen(true)] out Condition? condition,
                [NotNullWhen(false)] out string? errorMessage)
            {
                if (args is not [var x, var y])
                {
                    return ArgumentAmtMismatch(args.Count, 2, out condition, out errorMessage);
                }

                return Ok(new VecCondition(x, y), out condition, out errorMessage);
            }
        }

        private struct ClampFunc : IPureFunc<float, float, float, float>
        {
            public static float Get(float xVal, float minVal, float maxVal)
            {
                if (minVal > maxVal)
                    return Math.Clamp(xVal, maxVal, minVal);
                return Math.Clamp(xVal, minVal, maxVal);
            }

            public static string Arg1Name => "x";

            public static string Arg2Name => "minVal";

            public static string Arg3Name => "maxVal";
        }

        private sealed class DialogCondition(FrostConditionRuntime runtime, Condition key) : FunctionCondition(key)
        {
            public static bool TryCreate(FrostConditionRuntime runtime, IReadOnlyList<Condition> args,
                [NotNullWhen(true)] out Condition? condition, [NotNullWhen(false)] out string? errorMessage)
            {
                if (args is not [var x])
                {
                    return TooFewArgs(args.Count, 1, out condition, out errorMessage);
                }

                return Ok(new DialogCondition(runtime, x), out condition, out errorMessage);
            }

            public override object Get(Session session, object? userdata)
            {
                var name = key.GetString(session, userdata);

                return ((Celeste.Game.Content.Dialogs.IOriginalGameTextEnvironment)runtime.Engine).Text
                    .CleanDialog(name);
            }

            protected internal override Type ReturnType => typeof(string);
        }

        private sealed class IfCondition(Condition condition, Condition ifTrue, Condition ifFalse)
            : FunctionCondition(condition, ifTrue)
        {
            private readonly Condition _condition = condition;
            private readonly Condition _ifTrue = ifTrue;
            private readonly Condition _ifFalse = ifFalse;

            private static readonly FieldInfo ConditionFieldInfo
                = typeof(IfCondition).GetField(nameof(_condition),
                    BindingFlags.Instance | BindingFlags.NonPublic)!;

            private static readonly FieldInfo IfTrueFieldInfo
                = typeof(IfCondition).GetField(nameof(_ifTrue),
                    BindingFlags.Instance | BindingFlags.NonPublic)!;

            private static readonly FieldInfo IfFalseFieldInfo
                = typeof(IfCondition).GetField(nameof(_ifFalse),
                    BindingFlags.Instance | BindingFlags.NonPublic)!;

            protected internal override Type ReturnType => _ifFalse.ReturnType == _ifTrue.ReturnType
                ? _ifFalse.ReturnType ?? typeof(object)
                : typeof(object);

            public override object Get(Session session, object? userdata)
            {
                return _condition.Check(session, userdata)
                    ? _ifTrue.Get(session, userdata)
                    : _ifFalse.Get(session, userdata);
            }

            internal override void Emit(ConditionCompilationCtx ctx, Type targetType)
            {
                var il = ctx.Il;
                var labelEnd = il.DefineLabel();
                var labelFalseBranch = il.DefineLabel();
                var retType = ReturnType;

                LocalBuilder? tempOrigCond = null;

                if (UsesCurrentConditionLocalInEmit)
                {
                    il.EmitSaveCurrentCondition(ref tempOrigCond, ctx);
                }

                il.EmitSwapOutCurrentCondition(ref tempOrigCond, ctx, _condition, ConditionFieldInfo);
                _condition.Emit(ctx, typeof(bool));
                il.Emit(OpCodes.Brfalse, labelFalseBranch);

                il.EmitSwapOutCurrentCondition(ref tempOrigCond, ctx, _ifTrue, IfTrueFieldInfo);
                _ifTrue.Emit(ctx, targetType);
                il.Emit(OpCodes.Br, labelEnd);

                il.MarkLabel(labelFalseBranch);
                il.EmitSwapOutCurrentCondition(ref tempOrigCond, ctx, _ifFalse, IfFalseFieldInfo);
                _ifFalse.Emit(ctx, targetType);

                il.MarkLabel(labelEnd);
                il.EmitRevertCurrentCondition(tempOrigCond, ctx);
            }

            internal override bool UsesCurrentConditionLocalInEmit =>
                _condition.UsesCurrentConditionLocalInEmit
                || _ifTrue.UsesCurrentConditionLocalInEmit
                || _ifFalse.UsesCurrentConditionLocalInEmit;

            public static bool TryCreate(IReadOnlyList<Condition> args, [NotNullWhen(true)] out Condition? condition,
                [NotNullWhen(false)] out string? errorMessage)
            {
                if (args is not [var cond, var ifTrue, var ifFalse])
                {
                    return ArgumentAmtMismatch(args.Count, 3, out condition, out errorMessage);
                }

                return Ok(new IfCondition(cond, ifTrue, ifFalse), out condition, out errorMessage);
            }
        }

        internal abstract class FunctionCondition : Condition
        {
            protected readonly Condition[] Conditions;

            public FunctionCondition(params IEnumerable<Condition> conditions)
            {
                Conditions = conditions.ToArray();
            }

            protected override IEnumerable<object> GetArgsForDebugPrint() => Conditions;

            protected internal static bool ArgumentAmtMismatch(int received, int expected,
                out Condition? condition, [NotNullWhen(false)] out string? errorMessage)
            {
                condition = null;
                if (received > expected)
                {
                    errorMessage = $"Too many arguments: {received}, expected: {expected}";
                    return false;
                }

                if (received < expected)
                {
                    errorMessage = $"Too few arguments: {received}, expected: {expected}";
                    return false;
                }

                errorMessage = null;
                return true;
            }

            protected internal static bool TooFewArgs(int received, int expected,
                out Condition? condition, [NotNullWhen(false)] out string? errorMessage)
            {
                condition = null;
                if (received < expected)
                {
                    errorMessage = $"Too few arguments: {received}, expected: {expected}";
                    return false;
                }

                errorMessage = null;
                return true;
            }

            protected internal static bool Ok(Condition condition, [NotNullWhen(true)] out Condition? retCond,
                [NotNullWhen(false)] out string? errorMessage)
            {
                retCond = condition;
                errorMessage = null;
                return true;
            }
        }
    }
}
