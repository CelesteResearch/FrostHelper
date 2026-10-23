using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Numerics;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Conditions;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Compilation;
using Session = Celeste.Simulation.Runtime.Session.ChapterCompletionSession;

namespace Celeste.Mods.FrostHelper.Runtime.Expressions.Operators
{
    internal sealed class StringInterpolationOperator(List<Condition> args) : Condition
    {
        private Condition GetArg(int index) => args[index];

        private static readonly MethodInfo MethodGetArg =
            typeof(StringInterpolationOperator).GetMethod(nameof(GetArg),
                BindingFlags.Instance | BindingFlags.NonPublic)!;

        private static readonly MethodInfo MethodInterpolatorHandlerAppendLiteralString
            = typeof(ExpressionInterpolator.Handler).GetMethod(nameof(ExpressionInterpolator.Handler.AppendLiteral),
                BindingFlags.Instance | BindingFlags.Public, null, [typeof(string)], null)!;

        private static readonly MethodInfo MethodInterpolatorHandlerAppendFormattedObject
            = typeof(ExpressionInterpolator.Handler).GetMethod(nameof(ExpressionInterpolator.Handler.AppendFormatted),
                BindingFlags.Instance | BindingFlags.Public, null, [typeof(object)], null)!;

        private static readonly MethodInfo MethodAppendVector =
            typeof(ExpressionInterpolator.Handler).GetMethod(nameof(ExpressionInterpolator.Handler.AppendFormatted),
                [typeof(Vector2)])!;

        private static readonly MethodInfo MethodAppendSpan = typeof(ExpressionInterpolator.Handler).GetMethods()
            .Single(m =>
                m.Name == nameof(ExpressionInterpolator.Handler.AppendFormatted) && m.IsGenericMethodDefinition);

        private static readonly MethodInfo MethodInterpolatorHandlerResultToString
            = typeof(ExpressionInterpolator.Handler).GetMethod(nameof(ExpressionInterpolator.Handler.ResultToString),
                BindingFlags.Instance | BindingFlags.Public)!;


        public override object Get(Session session, object? userdata)
        {
            ExpressionInterpolator.Handler handler =
                new ExpressionInterpolator.Handler(0, args.Count, ExpressionInterpolator.Shared);
            foreach (var arg in args)
            {
                var obj = arg.Get(session, userdata);
                if (obj is string str)
                    handler.AppendLiteral(str);
                else
                    handler.AppendFormatted(obj);
            }

            return handler.ResultToString();
        }

        internal override void Emit(ConditionCompilationCtx ctx, Type targetType)
        {
            var il = ctx.Il;
            var handlerLocal = il.DeclareLocal(typeof(ExpressionInterpolator.Handler));
            LocalBuilder? tempLocal = null;
            il.Emit(OpCodes.Ldloca, handlerLocal);
            il.Emit(OpCodes.Ldc_I4_0); // literal length
            il.Emit(OpCodes.Ldc_I4, args.Count); // formatted length
            il.Emit(OpCodes.Call,
                typeof(ExpressionInterpolator).GetProperty(nameof(ExpressionInterpolator.Shared))!.GetMethod!);
            il.Emit(OpCodes.Call,
                typeof(ExpressionInterpolator.Handler).GetConstructor([
                    typeof(int), typeof(int), typeof(ExpressionInterpolator)
                ])!);

            var argI = 0;
            foreach (var arg in args)
            {
                il.EmitSwapOutCurrentCondition(ref tempLocal, ctx, arg, () =>
                {
                    il.Emit(OpCodes.Ldloc, tempLocal!);
                    il.Emit(OpCodes.Ldc_I4, argI);
                    il.Emit(OpCodes.Call, MethodGetArg);
                });

                il.Emit(OpCodes.Ldloca, handlerLocal);

                var argType = arg.ReturnType ?? typeof(object);
                arg.Emit(ctx, argType);
                if (argType == typeof(string))
                {
                    il.Emit(OpCodes.Call, MethodInterpolatorHandlerAppendLiteralString);
                }
                else if (argType == typeof(Vector2))
                {
                    il.Emit(OpCodes.Call, MethodAppendVector);
                }
                else if (SpanFormattingSupport.Supports(argType))
                {
                    SpanFormattingSupport.Prepare(argType);
                    il.Emit(OpCodes.Call,
                        MethodAppendSpan.MakeGenericMethod(argType));
                }
                else
                {
                    if (argType.IsValueType)
                        il.Emit(OpCodes.Box, argType);
                    il.Emit(OpCodes.Call, MethodInterpolatorHandlerAppendFormattedObject);
                }

                argI++;
            }

            il.EmitRevertCurrentCondition(tempLocal, ctx);

            il.Emit(OpCodes.Ldloca, handlerLocal);
            il.Emit(OpCodes.Call, MethodInterpolatorHandlerResultToString);
            il.EmitConvertToInSessionExpression(typeof(string), targetType);
        }

        internal override bool UsesCurrentConditionLocalInEmit { get; }
            = args.Any(a => a.UsesCurrentConditionLocalInEmit);

        protected internal override Type ReturnType => typeof(string);

        protected override IEnumerable<object> GetArgsForDebugPrint() => args;
    }
}
