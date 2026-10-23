using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;
using MonoMod.Utils;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Conditions;
using Session = Celeste.Simulation.Runtime.Session.ChapterCompletionSession;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Celeste.Mods.FrostHelper.Runtime.Expressions.Compilation
{
    using Color = Celeste.Core.Display.PackedColor;

    internal static class ExpressionIlGeneratorExtensions
    {
        private static readonly MethodInfo MethodColorHelperHexToColorInt =
            typeof(ExpressionColorConversions).GetMethod(nameof(ExpressionColorConversions.HexToColorInt),
                [typeof(int)])!;

        private static readonly MethodInfo MethodColorHelperGetColor =
            typeof(ExpressionColorConversions).GetMethod(nameof(ExpressionColorConversions.GetColor),
                new[] { typeof(string), typeof(FrostConditionRuntime) })!;

        private static readonly MethodInfo MethodColorHelperColorToHex =
            typeof(ExpressionColorConversions).GetMethod(nameof(ExpressionColorConversions.ColorToHex), [typeof(Color)])
            !;

        private static readonly MethodInfo MethodColorHelperColorToHexInt =
            typeof(ExpressionColorConversions).GetMethod(nameof(ExpressionColorConversions.ColorToHexInt),
                [typeof(Color)])!;


        public static void EmitLoadConstAs(this ILGenerator il, object value, Type targetType)
        {
            object? changedType;
            try
            {
                changedType = Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
            }
            catch (InvalidCastException)
            {
                // Not directly castable, we'll instead do the cast at runtime.
                il.EmitLoadConstAs(value, value.GetType());
                il.EmitConvertToInSessionExpression(value.GetType(), targetType);
                return;
            }

            switch (changedType)
            {
                case int i:
                    il.Emit(OpCodes.Ldc_I4, i);
                    break;
                case float f:
                    il.Emit(OpCodes.Ldc_R4, f);
                    break;
                case long i:
                    il.Emit(OpCodes.Ldc_I8, i);
                    break;
                case double f:
                    il.Emit(OpCodes.Ldc_R8, f);
                    break;
                case string s:
                    il.Emit(OpCodes.Ldstr, s);
                    break;
                case false:
                    il.Emit(OpCodes.Ldc_I4_0);
                    break;
                case true:
                    il.Emit(OpCodes.Ldc_I4_1);
                    break;
                default:
                    throw new Exception($"Cannot convert {value.GetType()} to {targetType}");
            }

            if (targetType == typeof(object) && changedType.GetType().IsValueType)
            {
                il.Emit(OpCodes.Box, changedType.GetType());
            }
        }

        public static void EmitConvertToInSessionExpression(this ILGenerator il, Type fromType, Type toType)
        {
            if (toType == fromType)
                return;

            if (fromType == typeof(bool))
            {
                // In session expressions, booleans get coerced to 1/0.
                il.Emit(OpCodes.Ldc_I4_1);
                il.Emit(OpCodes.Ceq);
                fromType = typeof(int);
            }

            if (toType == fromType)
                return;

            if (toType == typeof(object))
            {
                if (fromType.IsValueType)
                {
                    il.Emit(OpCodes.Box, fromType);
                }

                return;
            }

            if (fromType == typeof(bool))
            {
                if (toType == typeof(int))
                {
                    var label = il.DefineLabel();
                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(OpCodes.Brfalse, label);
                    il.Emit(OpCodes.Ldc_I4_1);
                    il.MarkLabel(label);
                    return;
                }
            }

            if (fromType == typeof(byte))
            {
                if (toType == typeof(int))
                {
                    return;
                }
            }

            if (fromType == typeof(int))
            {
                if (toType == typeof(float))
                {
                    il.Emit(OpCodes.Conv_R4);
                    return;
                }
            }

            if (fromType == typeof(float))
            {
                if (toType == typeof(int))
                {
                    il.Emit(OpCodes.Call,
                        typeof(ExpressionNumericConversions).GetMethod(
                            nameof(ExpressionNumericConversions.OriginalCastToInt32), new[] { typeof(float) })!);
                    return;
                }
            }

            if (fromType == typeof(object))
            {
                if (toType == typeof(bool))
                {
                    il.Emit(OpCodes.Call, typeof(Condition).GetMethod(nameof(Condition.CoerceToBool))!);
                    return;
                }

                if (toType == typeof(float) || toType == typeof(int))
                {
                    il.Emit(OpCodes.Ldarg_3);
                    il.Emit(OpCodes.Call,
                        typeof(Condition).GetMethod(nameof(Condition.CoerceToNumber))!.MakeGenericMethod(toType));
                    return;
                }
            }

            if (toType == typeof(bool))
            {
                if (fromType == typeof(int))
                {
                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(OpCodes.Cgt_Un);
                    return;
                }

                if (fromType == typeof(float))
                {
                    il.Emit(OpCodes.Ldc_R4, 0f);
                    il.Emit(OpCodes.Ceq);
                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(OpCodes.Ceq);
                    return;
                }
            }

            if (toType == typeof(Color))
            {
                if (fromType == typeof(int))
                {
                    il.Emit(OpCodes.Call, MethodColorHelperHexToColorInt);
                    return;
                }

                if (fromType == typeof(float))
                {
                    il.Emit(OpCodes.Call,
                        typeof(ExpressionNumericConversions).GetMethod(
                            nameof(ExpressionNumericConversions.OriginalCastToInt32), new[] { typeof(float) })!);
                    il.Emit(OpCodes.Call, MethodColorHelperHexToColorInt);
                    return;
                }

                if (fromType == typeof(string))
                {
                    il.Emit(OpCodes.Ldarg_3);
                    il.Emit(OpCodes.Call, MethodColorHelperGetColor);
                    return;
                }
            }

            if (fromType == typeof(Color))
            {
                // Color -> string is hex code
                if (toType == typeof(string))
                {
                    il.Emit(OpCodes.Call, MethodColorHelperColorToHex);
                    return;
                }

                if (toType == typeof(int))
                {
                    il.Emit(OpCodes.Call, MethodColorHelperColorToHexInt);
                    return;
                }
            }

            if (toType.IsAssignableFrom(fromType))
                return;

            throw new Exception($"Cannot convert {fromType} to {toType}");
        }

        public static void EmitSaveCurrentCondition(this ILGenerator il, [NotNull] ref LocalBuilder? tempOrigCond,
            ConditionCompilationCtx ctx)
        {
            if (tempOrigCond is null)
            {
                tempOrigCond = il.DeclareLocal(typeof(Condition));
                il.Emit(OpCodes.Ldloc, ctx.CurrentCondition);
                il.Emit(OpCodes.Stloc, tempOrigCond);
            }
        }

        public static void EmitSwapOutCurrentCondition(this ILGenerator il, ref LocalBuilder? tempOrigCond,
            ConditionCompilationCtx ctx, Condition next, FieldInfo fieldContainingNext)
        {
            if (!next.UsesCurrentConditionLocalInEmit)
                return;

            il.EmitSaveCurrentCondition(ref tempOrigCond, ctx);

            il.Emit(OpCodes.Ldloc, tempOrigCond);
            il.Emit(OpCodes.Castclass, fieldContainingNext.DeclaringType!);
            il.Emit(OpCodes.Ldfld, fieldContainingNext);
            il.Emit(OpCodes.Stloc, ctx.CurrentCondition);
        }

        public static void EmitSwapOutCurrentCondition(this ILGenerator il, ref LocalBuilder? tempOrigCond,
            ConditionCompilationCtx ctx, Condition next, Action emitLoadCondition)
        {
            if (!next.UsesCurrentConditionLocalInEmit)
                return;

            if (tempOrigCond is null)
            {
                tempOrigCond = il.DeclareLocal(typeof(Condition));
                il.Emit(OpCodes.Ldloc, ctx.CurrentCondition);
                il.Emit(OpCodes.Stloc, tempOrigCond);
            }

            emitLoadCondition();
            il.Emit(OpCodes.Stloc, ctx.CurrentCondition);
        }

        public static void EmitRevertCurrentCondition(this ILGenerator il, LocalBuilder? tempOrigCond,
            ConditionCompilationCtx ctx)
        {
            if (tempOrigCond is not null)
            {
                il.Emit(OpCodes.Ldloc, tempOrigCond);
                il.Emit(OpCodes.Stloc, ctx.CurrentCondition);
            }
        }

        public static void EmitLdlocOrLdloca(this ILGenerator il, LocalBuilder local)
        {
            if (local.LocalType.IsValueType)
            {
                il.Emit(OpCodes.Ldloca, local);
                return;
            }

            il.Emit(OpCodes.Ldloc, local);
        }
    }
}
