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
        private readonly FrostConditionRuntime runtime;

        public FunctionCommands(FrostConditionRuntime runtime)
        {
            this.runtime = runtime;
            Register("min", [ArgumentDescriptor.VarargFor(TypeDescriptor.For(typeof(float)))],
                TypeDescriptor.For(typeof(float)),
                [RenderPart.Default("Returns the smallest value from all provided arguments.")],
                MinCondition.TryCreate);

            Register("max", [ArgumentDescriptor.VarargFor(TypeDescriptor.For(typeof(float)))],
                TypeDescriptor.For(typeof(float)),
                [RenderPart.Default("Returns the largest value from all provided arguments.")], MaxCondition.TryCreate);

            RegisterPure<float, float, float, float, ClampFunc>("clamp",
                [RenderPart.Default("Clamps the value of x so that its between min and max.")]);
            RegisterPure<float, float, AbsFunc>("abs", [RenderPart.Default("Returns the absolute value of x.")]);

            RegisterPure<float, float, SinFunc>("sin",
            [
                RenderPart.Default("Calculates trigonometrical functions, x is assumed to be in radians. (Tip: $pi)")
            ]);
            RegisterPure<float, float, CosFunc>("cos",
            [
                RenderPart.Default("Calculates trigonometrical functions, x is assumed to be in radians. (Tip: $pi)")
            ]);
            RegisterPure<float, float, TanFunc>("tan",
            [
                RenderPart.Default("Calculates trigonometrical functions, x is assumed to be in radians. (Tip: $pi)")
            ]);

            RegisterPure<float, float, TruncateFunc>("truncate", [RenderPart.Default("Truncates the value.")]);
            RegisterPure<float, float, RoundFunc>("round", [RenderPart.Default("Rounds the value.")]);
            RegisterPure<float, float, FloorFunc>("floor", [RenderPart.Default("Gets the floor of the value.")]);
            RegisterPure<float, float, CeilingFunc>("ceiling", [RenderPart.Default("Gets the ceiling of the value.")]);

            RegisterPure<float, float, float, PowFunc>("pow", [RenderPart.Default("x raised to the power of y.")]);
            RegisterPure<float, float, Pow2Func>("pow2", [RenderPart.Default("x raised to the power of 2.")]);

            RegisterPure<float, float, SqrtFunc>("sqrt", [RenderPart.Default("x raised to the power of 2.")]);
            RegisterPure<float, float, CbrtFunc>("cbrt", [RenderPart.Default("Square root of x.")]);
            RegisterPure<float, float, ExpFunc>("exp", [RenderPart.Default("Cube root of x.")]);
            RegisterPure<float, float, Exp2Func>("exp2", [RenderPart.Default("$e raised to the power of x")]);

            RegisterPure<float, float, float, LogFunc>("log", [RenderPart.Default("The base-y logarithm of x.")]);
            RegisterPure<float, float, LognFunc>("logn", [RenderPart.Default("The natural logarithm of x.")]);
            RegisterPure<float, float, Log2Func>("log2", [RenderPart.Default("The base-2 logarithm of x.")]);
            RegisterPure<float, float, Log10Func>("log10", [RenderPart.Default("The base-10 logarithm of x.")]);

            RegisterPure<float, float, float, float, LerpFunc>("lerp",
            [
                RenderPart.Default(
                    "Performs a linear interpolation between two values based on the given weight. Params: x — The first value, which is intended to be the lower bound. y — The second value, which is intended to be the upper bound. amount — A value between 0 and 1, that indicates the weight of the interpolation.")
            ]);
            RegisterPure<PackedColor, PackedColor, float, PackedColor, LerpCFunc>("lerpc",
            [
                RenderPart.Default(
                    "Performs a linear interpolation between two colors based on the given weight. Params: x — The first value, which is intended to be the lower bound. y — The second value, which is intended to be the upper bound. amount — A value between 0 and 1, that indicates the weight of the interpolation.")
            ]);

            RegisterPure<float, float, YoYoFunc>("yoyo",
                [RenderPart.Default("x <= 0.5 ? x * 2 : 1.0 - (value - 0.5) * 2.0).")]);

            RegisterPure<int, int, IEnumerable<int>, RangeFunc>("range", [
                RenderPart.Default("Creates a "),
                RenderPart.Type(TypeDescriptor.For(typeof(IEnumerable<int>))),
                RenderPart.Default(" containing numbers between min (inclusive) and max (exclusive).")
            ]);

            RegisterSession<string, IEnumerable<string>, FlagsFunc>("flags", [
                RenderPart.Default("Creates a "),
                RenderPart.Type(TypeDescriptor.For(typeof(IEnumerable<string>))),
                RenderPart.Default(" containing all currently set flags matching the given regex.")
            ]);

            RegisterPure<int, int, int, PackedColor, RgbFunc>("rgb", [
                RenderPart.Default("Creates a "),
                RenderPart.Type(TypeDescriptor.For(typeof(PackedColor))),
                RenderPart.Default(" using r, g, b values, assumed to be in range 0-255.")
            ]);

            RegisterPure<int, int, int, int, PackedColor, RgbaFunc>("rgba", [
                RenderPart.Default("Creates a "),
                RenderPart.Type(TypeDescriptor.For(typeof(PackedColor))),
                RenderPart.Default(" using r, g, b, a values, assumed to be in range 0-255.")
            ]);

            RegisterPure<float, float, float, PackedColor, HsvFunc>("hsv", [
                RenderPart.Default("Creates a "),
                RenderPart.Type(TypeDescriptor.For(typeof(PackedColor))),
                RenderPart.Default(" using h, s, v values, assumed to be in range 0-1.")
            ]);

            RegisterPure<float, float, float, int, PackedColor, HsvaFunc>("hsva", [
                RenderPart.Default("Creates a "),
                RenderPart.Type(TypeDescriptor.For(typeof(PackedColor))),
                RenderPart.Default(" using h, s, v values, assumed to be in range 0-1. Alpha is in range 0-255.")
            ]);

            Register("dialog", [new ArgumentDescriptor("dialogId", TypeDescriptor.For(typeof(string)))],
                TypeDescriptor.For(typeof(string)),
                [RenderPart.Default("Gets the dialog text in the current language for the given dialogId.")],
                (IReadOnlyList<Condition> args, out Condition? result, out string? error) =>
                    DialogCondition.TryCreate(runtime, args, out result, out error));

            Register("vec", [
                    new ArgumentDescriptor("x", TypeDescriptor.For(typeof(float))),
                    new ArgumentDescriptor("y", TypeDescriptor.For(typeof(float)))
                ],
                TypeDescriptor.For(typeof(Vector2)),
                [
                    RenderPart.Default("Creates a "),
                    RenderPart.Type(TypeDescriptor.For(typeof(Vector2))),
                    RenderPart.Default(" with the given x, y values.")
                ], VecCondition.TryCreate);


            Register("if", [
                    new ArgumentDescriptor("condition", TypeDescriptor.For(typeof(bool))),
                    new ArgumentDescriptor("ifTrue", TypeDescriptor.Any),
                    new ArgumentDescriptor("ifFalse", TypeDescriptor.Any),
                ],
                TypeDescriptor.Any,
                [
                    RenderPart.Default(
                        "Returns the value of the 2nd argument if the 1st argument is true, otherwise returns the 3rd argument.")
                ],
                IfCondition.TryCreate);
        }

        internal readonly Dictionary<string, FunctionCommand> Registry = new();

        private void Register(string name, IReadOnlyList<ArgumentDescriptor> arguments, TypeDescriptor returnType,
            IReadOnlyList<RenderPart> description, FunctionCommandFactory factory)
        {
            Registry[name] = new FunctionCommand(factory, new CommandDescriptor
            {
                Name = name,
                Description = description,
                Arguments = arguments,
                ReturnType = returnType,
            });
        }

        private void RegisterPure<TArg1, TRet, TOp>(string name, IReadOnlyList<RenderPart> description)
            where TOp : struct, IPureFunc<TArg1, TRet>
        {
            Register(name, [
                    new ArgumentDescriptor(OperationMetadata<TOp>.GetArgumentName("ArgName"),
                        TypeDescriptor.For(typeof(TArg1)))
                ],
                TypeDescriptor.For(typeof(TRet)),
                description,
                PureMathCondition.TryCreate<TArg1, TRet, TOp>);
        }

        private void RegisterSession<TArg1, TRet, TOp>(string name, IReadOnlyList<RenderPart> description)
            where TOp : struct, ISessionFunc<TArg1, TRet>
        {
            Register(name, [
                    new ArgumentDescriptor(OperationMetadata<TOp>.GetArgumentName("ArgName"),
                        TypeDescriptor.For(typeof(TArg1)))
                ],
                TypeDescriptor.For(typeof(TRet)),
                description,
                PureMathCondition.TryCreateSession<TArg1, TRet, TOp>);
        }

        private void RegisterPure<TArg1, TArg2, TRet, TOp>(string name, IReadOnlyList<RenderPart> description)
            where TOp : struct, IPureFunc<TArg1, TArg2, TRet>
        {
            Register(name, [
                    new ArgumentDescriptor(OperationMetadata<TOp>.GetArgumentName("Arg1Name"),
                        TypeDescriptor.For(typeof(TArg1))),
                    new ArgumentDescriptor(OperationMetadata<TOp>.GetArgumentName("Arg2Name"),
                        TypeDescriptor.For(typeof(TArg2))),
                ],
                TypeDescriptor.For(typeof(TRet)),
                description,
                PureMathCondition.TryCreate<TArg1, TArg2, TRet, TOp>);
        }

        private void RegisterPure<TArg1, TArg2, TArg3, TRet, TOp>(string name, IReadOnlyList<RenderPart> description)
            where TOp : struct, IPureFunc<TArg1, TArg2, TArg3, TRet>
        {
            Register(name, [
                    new ArgumentDescriptor(OperationMetadata<TOp>.GetArgumentName("Arg1Name"),
                        TypeDescriptor.For(typeof(TArg1))),
                    new ArgumentDescriptor(OperationMetadata<TOp>.GetArgumentName("Arg2Name"),
                        TypeDescriptor.For(typeof(TArg2))),
                    new ArgumentDescriptor(OperationMetadata<TOp>.GetArgumentName("Arg3Name"),
                        TypeDescriptor.For(typeof(TArg3)))
                ],
                TypeDescriptor.For(typeof(TRet)),
                description,
                PureMathCondition.TryCreate<TArg1, TArg2, TArg3, TRet, TOp>);
        }

        private void RegisterPure<TArg1, TArg2, TArg3, TArg4, TRet, TOp>(string name,
            IReadOnlyList<RenderPart> description)
            where TOp : struct, IPureFunc<TArg1, TArg2, TArg3, TArg4, TRet>
        {
            Register(name, [
                    new ArgumentDescriptor(OperationMetadata<TOp>.GetArgumentName("Arg1Name"),
                        TypeDescriptor.For(typeof(TArg1))),
                    new ArgumentDescriptor(OperationMetadata<TOp>.GetArgumentName("Arg2Name"),
                        TypeDescriptor.For(typeof(TArg2))),
                    new ArgumentDescriptor(OperationMetadata<TOp>.GetArgumentName("Arg3Name"),
                        TypeDescriptor.For(typeof(TArg3))),
                    new ArgumentDescriptor(OperationMetadata<TOp>.GetArgumentName("Arg4Name"),
                        TypeDescriptor.For(typeof(TArg4)))
                ],
                TypeDescriptor.For(typeof(TRet)),
                description,
                PureMathCondition.TryCreate<TArg1, TArg2, TArg3, TArg4, TRet, TOp>);
        }

        internal struct RangeFunc : IPureFunc<int, int, IEnumerable<int>>
        {
            public static IEnumerable<int> Get(int min, int count)
            {
                return Enumerable.Range(min, count);
            }

            public static string Arg1Name => "min";

            public static string Arg2Name => "max";
        }

        public void Register(string modName, string cmdName, Func<Session, object?, IReadOnlyList<object>, object> func)
        {
            var key = $"{modName}.{cmdName}";
            if (Registry.TryGetValue(key, out var existing))
            {
                ((Celeste.Everest.Diagnostics.Logging.IEverestLoggerProcessEnvironment)runtime.Engine).Logger.Warn(
                    "FrostHelper.ConditionHelper", $"Replacing function command '${key}'");
            }

            Registry[key] = new FunctionCommand(CreateFactoryForCustomCommand(func), new CommandDescriptor
            {
                Name = key,
                DeclaringMod = modName,
                ReturnType = TypeDescriptor.Any
            });
        }

        internal static FunctionCommandFactory CreateFactoryForCustomCommand(
            Func<Session, object?, IReadOnlyList<object>, object> func)
        {
            return (args, out result, out message) =>
            {
                result = new ModFunctionCondition(args, func);
                message = null;
                return true;
            };
        }

        public bool TryCreate(string name, IReadOnlyList<Condition> args, IExpressionContext ctx,
            [NotNullWhen(true)] out Condition? condition)
        {
            if (!ctx.TryGetFunctionCommand(name, out var functionCommand) &&
                !Registry.TryGetValue(name, out functionCommand))
            {
                if (name.Contains('.'))
                {
                    var remaining = name;
                    List<string>? fields = null;
                    condition = null;
                    while (true)
                    {
                        // Try simple commands from the context
                        if (ctx.TryGetSimpleCommand(remaining, out var cond))
                        {
                            condition = cond;
                            break;
                        }

                        // Try simple commands
                        if (runtime.SimpleCommands.Registry.TryGetValue(remaining, out var simpleCommand))
                        {
                            condition = simpleCommand.Condition;
                            break;
                        }

                        var lastDotIdx = remaining.LastIndexOf('.');
                        if (lastDotIdx == -1)
                            break;
                        fields ??= [];
                        fields.Add(remaining[(lastDotIdx + 1)..]);
                        remaining = remaining[..lastDotIdx];
                    }

                    if (condition is not null)
                    {
                        while (fields?.Count > 1)
                        {
                            condition = runtime.FieldAccessCommands.Create(fields[^1], condition, ctx);
                            fields.RemoveAt(fields.Count - 1);
                        }

                        if (fields?.Count > 0)
                        {
                            condition = runtime.InstanceFunctionCommands.Create(fields[0], condition, args, ctx);
                            return true;
                        }
                    }
                }

                runtime.Notify($"Unknown Session Expression function: '{name}'");
                condition = null;
                return false;
            }

            if (!functionCommand.Factory(args, out condition, out var errorMessage))
            {
                runtime.Notify($"Failed to create Session Expression function: '{name}':\n{errorMessage}");
                condition = null;
                return false;
            }

            condition.Runtime = runtime;
            condition.Descriptor = functionCommand.Descriptor;

            return true;
        }

        private sealed class ModFunctionCondition(
            IReadOnlyList<Condition> args,
            Func<Session, object?, IReadOnlyList<object>, object> func)
            : FunctionCondition(args)
        {
            private readonly object[] _array = new object[args.Count];
            //private readonly LazyFunctionArgumentList _args = new(args);

            public override object Get(Session session, object? userdata)
            {
                for (int i = 0; i < args.Count; i++)
                {
                    _array[i] = args[i].Get(session, userdata);
                }

                return func(session, userdata, _array);
                // TODO: test!
                //_args.Reset(session, userdata);
                //return func(session, userdata, _args);
            }
        }
    }
}
