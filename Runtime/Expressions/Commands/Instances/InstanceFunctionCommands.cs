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
        private readonly FrostConditionRuntime runtime;
        internal readonly HashSet<string> NotifiedInvalidRegexes = [];

        public InstanceFunctionCommands(FrostConditionRuntime runtime)
        {
            this.runtime = runtime;
            Register<object, string, string, Str>("str", [RenderPart.Default("Converts the value to a string.")]);
            Register<string, string, int, StringIsMatch>("isMatch",
                [RenderPart.Default("Checks whether the string matches the given regex.")]);

            RegisterSession<IEnumerable, LambdaCondition, float, EnumerableSum>("sum", [
                RenderPart.Default("""
                                   Calculates the sum of the results of applying the callback to every element in the collection.
                                   The lambda receives one argument, set to the element to evaluate.
                                   """)
            ]);
            RegisterSession<IEnumerable, LambdaCondition, int, EnumerableAll>("all", [
                RenderPart.Default(
                    "Checks whether all elements in the collection match the given predicate.\nThe lambda receives one argument, set to the element to check.")
            ]);
            RegisterSession<IEnumerable, LambdaCondition, int, EnumerableAny>("any", [
                RenderPart.Default(
                    "Checks whether any element in the collection match the given predicate.\nThe lambda receives one argument, set to the element to check.")
            ]);
        }

        public delegate bool InstanceFunctionCommandFactory(
            Condition field, IReadOnlyList<Condition> args, [NotNullWhen(true)] out Condition? result,
            [NotNullWhen(false)] out string? errorMessage);

        internal readonly Dictionary<(Type, string), InstanceFunctionCommand> Functions = new();

        private void Register(string name, Type objType, IReadOnlyList<ArgumentDescriptor> arguments,
            TypeDescriptor returnType, IReadOnlyList<RenderPart> description, InstanceFunctionCommandFactory factory)
        {
            Functions[(objType, name)] = new InstanceFunctionCommand(factory, new CommandDescriptor
            {
                Name = name,
                Description = description,
                Arguments = arguments,
                ReturnType = returnType,
                DeclaringType = TypeDescriptor.For(objType),
            });
        }

        private void Register<TField, TArg, TResult, TOp>(string name, IReadOnlyList<RenderPart> description)
            where TOp : struct, IOneArgFunc<TField, TArg, TResult>
        {
            Register(name, typeof(TField), [
                    new ArgumentDescriptor(InstanceOperationMetadata<TOp>.ArgName, TypeDescriptor.For(typeof(TArg)))
                ],
                TypeDescriptor.For(typeof(TResult)),
                description,
                OneArgInstanceFunc<TField, TArg, TResult, TOp>.TryCreate);
        }

        private void RegisterSession<TField, TArg, TResult, TOp>(string name, IReadOnlyList<RenderPart> description)
            where TOp : struct, IOneArgSessionFunc<TField, TArg, TResult>
        {
            Register(name, typeof(TField), [
                    new ArgumentDescriptor(InstanceOperationMetadata<TOp>.ArgName, TypeDescriptor.For(typeof(TArg)))
                ],
                TypeDescriptor.For(typeof(TResult)),
                description,
                OneArgSessionInstanceFunc<TField, TArg, TResult, TOp>.TryCreate);
        }

        internal Condition Create(string functionName, Condition target, IReadOnlyList<Condition> arguments,
            IExpressionContext ctx)
        {
            if (target.ReturnType is { } knownType && GetFactory(knownType, functionName, ctx) is { } factory)
            {
                if (!factory.Factory(target, arguments, out var condition, out var errorMessage))
                {
                    ctx.Runtime.Notify(
                        $"Failed to create Session Expression function: '{functionName}', called on '{knownType}':\n{errorMessage}");
                    return new ConstInt(0);
                }

                condition.Runtime = ctx.Runtime;
                condition.Descriptor = factory.Descriptor;
                return condition;
            }

            return new DynamicInstanceFunction(functionName, target, arguments, ctx);
        }

        internal InstanceFunctionCommand? GetFactory(Type? type, string functionName, IExpressionContext ctx)
        {
            var currentType = type;
            while (currentType is not null)
            {
                if (Functions.TryGetValue((currentType, functionName), out var accessor))
                {
                    if (currentType != type)
                    {
                        Functions[(type!, functionName)] = accessor;
                    }

                    return accessor;
                }

                foreach (var interfaceType in currentType.GetInterfaces())
                {
                    if (Functions.TryGetValue((interfaceType, functionName), out accessor))
                    {
                        if (currentType != type)
                        {
                            Functions[(type!, functionName)] = accessor;
                        }

                        return accessor;
                    }
                }

                currentType = currentType.BaseType;
            }

            return null;
        }
    }
}
