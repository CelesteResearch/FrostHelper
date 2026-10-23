using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Celeste.Mods.FrostHelper.Runtime.Expressions;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Conditions;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Commands;
using Session = Celeste.Simulation.Runtime.Session.ChapterCompletionSession;
using Entity = Celeste.Scene.Entities.SceneEntityState;

namespace Celeste.Mods.FrostHelper.Runtime.Expressions.Contexts
{
    internal sealed class ExpressionContext : IExpressionContext
    {
        private readonly Dictionary<string, Condition> simpleCommands;
        private readonly Dictionary<string, FunctionCommand> functions;
        public FrostConditionRuntime Runtime { get; }
        public IReadOnlyDictionary<string, Condition> SimpleCommands => simpleCommands;
        public IReadOnlyDictionary<string, FunctionCommand> FunctionCommands => functions;

        public ExpressionContext(FrostConditionRuntime runtime, Dictionary<string, Condition> simpleCommands,
            Dictionary<string, FunctionCommand> functions)
        {
            Runtime = runtime;
            this.simpleCommands = simpleCommands;
            this.functions = functions;
        }

        public ExpressionContext CloneWith(Dictionary<string, Condition> commands,
            Dictionary<string, FunctionCommand> commandFunctions)
        {
            var newCommands = new Dictionary<string, Condition>(SimpleCommands);
            var newFunctions = new Dictionary<string, FunctionCommand>(FunctionCommands);
            foreach (var pair in commands) newCommands.Add(pair.Key, pair.Value);
            foreach (var pair in commandFunctions) newFunctions.Add(pair.Key, pair.Value);
            return new ExpressionContext(Runtime, newCommands, newFunctions);
        }

        public bool TryGetSimpleCommand(string name, [NotNullWhen(true)] out Condition? command) =>
            simpleCommands.TryGetValue(name, out command);

        public bool TryGetFunctionCommand(string name, [NotNullWhen(true)] out FunctionCommand? factory) =>
            functions.TryGetValue(name, out factory);

        public void RegisterSimpleCommand(string name, Condition command) => simpleCommands[name] = command;
        public void RegisterFunctionCommand(string name, FunctionCommand factory) => functions[name] = factory;
    }
}
