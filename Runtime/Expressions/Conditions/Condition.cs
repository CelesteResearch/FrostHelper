using Celeste.Mods.FrostHelper.Runtime.Expressions.Persistence;
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


    public abstract class Condition : ISavestatePersisted
    {
        internal FrostConditionRuntime Runtime { get; set; }
        internal CommandDescriptor? Descriptor { get; set; }

        internal string? SourceText { get; set; }

        internal static readonly object One = 1;
        internal static readonly object Zero = 0;

        protected object BoolToBoxedInt(bool b) => b ? One : Zero;

        public abstract object Get(Session session, object? userdata);

        protected virtual IEnumerable<object> GetArgsForDebugPrint() => [];

        protected internal virtual Type? ReturnType => null;

        protected internal bool ReturnTypeIsNumber => ReturnType == typeof(int) || ReturnType == typeof(float);

        private static readonly MethodInfo MethodGetT = typeof(Condition)
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).Single(method =>
                method.Name == nameof(Get) && method.IsGenericMethodDefinition && method.GetParameters().Length == 2);

        /// <summary>
        /// Emits IL code to execute this condition.
        /// The emitted code should assume its operating on an empty stack,
        /// and should leave exactly 1 value of type <paramref name="targetType"/> on top of the stack.
        ///
        /// Converting a value to <paramref name="targetType"/> is best done via calling <see cref="ConditionCompilationCtx.EmitConvertTo"/>.
        ///
        /// If this condition has any inner conditions nested within it, the best way to evaluate them is via:
        /// <code>
        ///     LocalBuilder? temp = null;
        ///     ctx.EmitSwapOutCurrentCondition(ref temp, nextCondition, ...);
        ///     nextCondition.Emit(ctx, type);
        ///     // ... potentially call EmitSwapOutCurrentCondition for other conditions ...
        ///     ctx.EmitRevertCurrentCondition(temp);
        ///     // ... do something with the value returned from nextCondition ...
        /// </code>
        ///
        /// Make sure to also override <see cref="UsesCurrentConditionLocalInEmit"/> if overriding this method.
        /// </summary>
        /// <param name="ctx">Context about the current compilation.</param>
        /// <param name="targetType">The type of value that should be left on the stack.</param>
        internal virtual void Emit(ConditionCompilationCtx ctx, Type targetType)
        {
            if (!UsesCurrentConditionLocalInEmit)
            {
                throw new Exception(
                    $"UsesCurrentConditionLocalInEmit is false, but CurrentCondition is being used by {GetType()}!");
            }

            ctx.EmitLoadCurrentCondition();
            ctx.EmitLoadSession();
            ctx.EmitLoadUserdata();
            ctx.Il.Emit(OpCodes.Callvirt, MethodGetT.MakeGenericMethod(targetType));
        }

        /// <summary>
        /// Whether the code emitted via <see cref="Emit"/> makes use of the CurrentCondition local variable.
        /// If true, additional boilerplate IL code needs to be emitted before evaluating this condition, so it's best to return false if possible.
        /// If this condition has any inner conditions nested within it, this method needs to return true if any of the child conditions return true. 
        /// </summary>
        internal virtual bool UsesCurrentConditionLocalInEmit => true;

        internal int GetInt(Session session, object? userdata = null)
        {
            return GetNumber<int>(session, userdata);
        }

        internal float GetFloat(Session session, object? userdata = null)
        {
            return GetNumber<float>(session, userdata);
        }

        internal T GetNumber<T>(Session session, object? userdata = null) where T : struct
        {
            var obj = Get(session, userdata);

            return CoerceToNumber<T>(obj, Runtime);
        }

        internal string GetString(Session session, object? userdata = null)
        {
            return CoerceToString(Get(session, userdata));
        }

        internal static string CoerceToString(object obj)
        {
            if (obj is string str)
                return str;
            if (obj is Vector2 vector)
                return "{X:" + vector.X.ToString("G6", CultureInfo.CurrentCulture) + " Y:" +
                       vector.Y.ToString("G6", CultureInfo.CurrentCulture) + "}";

            if (obj is Color c)
                return ExpressionColorConversions.ColorToHex(c);

            if (obj is IFormattable f)
                return f.ToString(null, CultureInfo.InvariantCulture);

            return obj.ToString() ?? "";
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal T Get<T>(Session session, object? userdata = null)
        {
            return Coerce<T>(Get(session, userdata), Runtime);
        }

        internal static object Coerce(object obj, Type toType, FrostConditionRuntime runtime)
        {
            if (toType == typeof(bool))
                return CoerceToBool(obj);
            if (toType == typeof(int))
                return CoerceToNumber<int>(obj, runtime);
            if (toType == typeof(float))
                return CoerceToNumber<float>(obj, runtime);
            if (toType == typeof(string))
                return CoerceToString(obj);
            if (toType == typeof(object))
                return obj;
            if (toType == typeof(Color))
                return CoerceToColor(obj, runtime);

            if (obj.GetType() == toType || toType.IsAssignableFrom(obj.GetType()))
            {
                return obj;
            }

            throw new ArgumentException($"Unsupported T for Session Expression: {toType.FullName}");
        }

        internal static T Coerce<T>(object obj, FrostConditionRuntime runtime)
        {
            if (typeof(T) == typeof(bool))
                return (T)(object)CoerceToBool(obj);
            if (typeof(T) == typeof(int))
                return (T)(object)CoerceToNumber<int>(obj, runtime);
            if (typeof(T) == typeof(float))
                return (T)(object)CoerceToNumber<float>(obj, runtime);
            if (typeof(T) == typeof(string))
                return (T)(object)CoerceToString(obj);
            if (typeof(T) == typeof(object))
                return (T)obj;
            if (typeof(T) == typeof(Color))
                return (T)(object)CoerceToColor(obj, runtime);

            if (obj.GetType() == typeof(T) || typeof(T).IsAssignableFrom(obj.GetType()))
            {
                return (T)obj;
            }

            throw new ArgumentException($"Unsupported T for Session Expression: {typeof(T).FullName}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Check(object? userdata = null) => Check(Runtime.CurrentLevel.Session, userdata);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Check(Session session, object? userdata = null) => CoerceToBool(Get(session, userdata));

        public bool Empty => this is EmptyCondition;

        public bool IsSimpleFlagCheck([NotNullWhen(true)] out string? checkedFlag)
        {
            if (this is FlagAccessor { Inverted: false, Flag: not null } f)
            {
                checkedFlag = f.Flag;
                return true;
            }

            checkedFlag = null;
            return false;
        }

        public virtual bool OnlyChecksFlags() => false;

        public static bool CoerceToBool(object obj)
        {
            return obj switch
            {
                bool b => b,
                int i => i != 0,
                float f => f != 0,
                null => false,
                _ => true,
            };
        }

        public static T CoerceToNumber<T>(object obj, FrostConditionRuntime runtime) where T : struct
        {
            if (obj is T t)
                return t;

            switch (obj)
            {
                case float f:
                    return ExpressionNumericConversions.CreateTruncating<T>(f);
                case double f:
                    return ExpressionNumericConversions.CreateTruncating<T>(f);
                case int f:
                    return ExpressionNumericConversions.CreateTruncating<T>(f);
                case short f:
                    return ExpressionNumericConversions.CreateTruncating<T>(f);
                case byte f:
                    return ExpressionNumericConversions.CreateTruncating<T>(f);
                case Color c:
                    return ExpressionNumericConversions.CreateTruncating<T>(ExpressionColorConversions
                        .ColorToHexInt(c));
            }

            runtime.Notify(
                $"Can't convert Session Expression value '{obj}' [{obj?.GetType().Name ?? "null"}] to {typeof(T).Name}.\nReturning 0!");
            return default;
        }

        public static Color CoerceToColor(object obj, FrostConditionRuntime runtime)
        {
            if (obj is Color clr)
                return clr;

            if (obj is string str)
                return runtime.Colors.Read(str);

            if (obj is int i)
                return ExpressionColorConversions.HexToColorInt(i);

            if (obj is float f)
                return ExpressionColorConversions.HexToColorInt(ExpressionNumericConversions.OriginalCastToInt32(f));

            runtime.Notify(
                $"Can't convert Session Expression value '{obj}' [{obj?.GetType().Name ?? "null"}] to Color.\nReturning 00000000!");
            return default;
        }

        public sealed override string ToString() => ToStringIndented("");

        private string ToStringIndented(string indent)
        {
            var args = GetArgsForDebugPrint().ToArray();
            var builder = new StringBuilder($"{indent}{GetType().Name}(");

            if (args is [])
            {
                builder.Append(')');
            }

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] is not Condition innerCond)
                {
                    builder.Append($"{args[i]}{(i + 1 < args.Length ? "," : ")")}");
                }
                else
                {
                    var nextIndent = "  " + indent;
                    builder.Append(
                        $"\n{innerCond.ToStringIndented(nextIndent)}{(i + 1 < args.Length ? "," : $"\n{indent})")}");
                }
            }

            return builder.ToString();
        }
    }
}
