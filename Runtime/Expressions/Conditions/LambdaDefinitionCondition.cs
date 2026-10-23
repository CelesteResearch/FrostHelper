using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Diagnostics.CodeAnalysis;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Operators;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Conditions;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Conditions.Consts;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Compilation;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Syntax;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Contexts;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Descriptors;
using Vector2 = System.Numerics.Vector2;
using Session = Celeste.Simulation.Runtime.Session.ChapterCompletionSession;

namespace Celeste.Mods.FrostHelper.Runtime.Expressions.Conditions
{
    using Color = Celeste.Core.Display.PackedColor;


    internal sealed class LambdaDefinitionCondition(IList<string> argumentNames) : Condition
    {
        internal Condition Code { get; set; }

        internal static readonly MethodInfo MethodCodeGet =
            typeof(LambdaDefinitionCondition).GetProperty(nameof(Code), BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetMethod!;

        public IList<string> ArgumentNames => argumentNames;

        public IList<Type?> ArgumentTypes { get; } = new Type[argumentNames.Count];

        public LambdaCondition Instance => field ??= new LambdaCondition(this);

        private static readonly MethodInfo MethodInstanceGet =
            typeof(LambdaDefinitionCondition).GetProperty(nameof(Instance))!.GetMethod!;

        public override object Get(Session session, object? userdata)
        {
            return Instance;
        }

        internal override void Emit(ConditionCompilationCtx ctx, Type targetType)
        {
            ctx.EmitLoadCurrentCondition<LambdaDefinitionCondition>();
            ctx.Il.Emit(OpCodes.Call, MethodInstanceGet);
            ctx.EmitConvertTo(typeof(LambdaCondition), targetType);
        }

        internal override bool UsesCurrentConditionLocalInEmit => true;

        protected internal override Type ReturnType => typeof(LambdaCondition);

        public Type GetArgumentType(int index)
        {
            return ArgumentTypes[index] ?? typeof(object);
        }
    }
}
