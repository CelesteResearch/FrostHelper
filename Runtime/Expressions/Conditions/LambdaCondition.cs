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


    internal sealed class LambdaCondition(LambdaDefinitionCondition definition) : Condition
    {
        private readonly object[] _args = new object[definition.ArgumentNames.Count];
        private readonly LambdaDefinitionCondition _definition = definition;

        private static readonly FieldInfo FieldDefinition =
            typeof(LambdaCondition).GetField(nameof(_definition), BindingFlags.Instance | BindingFlags.NonPublic)!;

        public int ArgumentCount => _args.Length;

        public object GetArgument(int index) => _args[index];

        public void SetArgument(int index, object value)
        {
            if (index < 0 || index >= _args.Length) return;
            _args[index] = value;
        }

        public override object Get(Session session, object? userdata)
        {
            return _definition.Code.Get(session, userdata);
        }

        private static readonly MethodInfo MethodSetArgument = typeof(LambdaCondition).GetMethod(nameof(SetArgument))!;

        public void EmitSetArgument(ConditionCompilationCtx ctx)
        {
            ctx.Il.Emit(OpCodes.Callvirt, MethodSetArgument);
        }

        internal override void Emit(ConditionCompilationCtx ctx, Type targetType)
        {
            LocalBuilder? temp = null;
            ctx.EmitSwapOutCurrentCondition(ref temp, _definition.Code, () =>
            {
                // CurrentField is this
                ctx.EmitLoadCurrentCondition<LambdaCondition>();
                ctx.Il.Emit(OpCodes.Ldfld, FieldDefinition);
                ctx.Il.Emit(OpCodes.Call, LambdaDefinitionCondition.MethodCodeGet);
            });
            _definition.Code.Emit(ctx, targetType);

            ctx.EmitRevertCurrentCondition(temp);
        }

        internal override bool UsesCurrentConditionLocalInEmit
            => _definition.Code.UsesCurrentConditionLocalInEmit;
    }
}
