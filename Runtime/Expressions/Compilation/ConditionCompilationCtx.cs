using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;
using MonoMod.Utils;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Conditions;
using Session = Celeste.Simulation.Runtime.Session.ChapterCompletionSession;

namespace Celeste.Mods.FrostHelper.Runtime.Expressions.Compilation
{
    internal sealed class ConditionCompilationCtx
    {
        private const int SessionArgId = 0;
        private const int UserdataArgId = 1;

        public LocalBuilder CurrentCondition { get; init; }

        public ILGenerator Il { get; init; }

        public void EmitLoadSession()
        {
            Il.Emit(OpCodes.Ldarg, SessionArgId);
        }

        public void EmitLoadCurrentCondition()
        {
            Il.Emit(OpCodes.Ldloc, CurrentCondition);
        }

        public void EmitLoadCurrentCondition<T>()
        {
            Il.Emit(OpCodes.Ldloc, CurrentCondition);
            Il.Emit(OpCodes.Castclass, typeof(T));
        }

        public void EmitLoadUserdata()
        {
            Il.Emit(OpCodes.Ldarg, UserdataArgId);
        }

        public void EmitLoadUserdata<T>()
        {
            Il.Emit(OpCodes.Ldarg, UserdataArgId);
            Il.Emit(OpCodes.Castclass, typeof(T));
        }

        public void EmitConvertTo(Type fromType, Type toType)
        {
            Il.EmitConvertToInSessionExpression(fromType, toType);
        }

        public void EmitSwapOutCurrentCondition(ref LocalBuilder? oldConditionTempLocal, Condition conditionToSwapTo,
            FieldInfo fieldStoringConditionToSwapTo)
        {
            Il.EmitSwapOutCurrentCondition(ref oldConditionTempLocal, this, conditionToSwapTo,
                fieldStoringConditionToSwapTo);
        }

        public void EmitSwapOutCurrentCondition(ref LocalBuilder? oldConditionTempLocal, Condition conditionToSwapTo,
            Action emitLoadConditionToSwapTo)
        {
            Il.EmitSwapOutCurrentCondition(ref oldConditionTempLocal, this, conditionToSwapTo,
                emitLoadConditionToSwapTo);
        }

        public void EmitRevertCurrentCondition(LocalBuilder? oldConditionTempLocal)
        {
            Il.EmitRevertCurrentCondition(oldConditionTempLocal, this);
        }

        public FrostConditionRuntime Runtime { get; init; }
        public void EmitLoadRuntime() => Il.Emit(OpCodes.Ldarg_3);

        public void EmitLoadScene()
        {
            EmitLoadRuntime();
            Il.Emit(OpCodes.Callvirt,
                typeof(FrostConditionRuntime).GetProperty(nameof(FrostConditionRuntime.CurrentScene)).GetMethod);
        }

        public void EmitLoadSettings()
        {
            EmitLoadRuntime();
            Il.Emit(OpCodes.Callvirt,
                typeof(FrostConditionRuntime).GetProperty(nameof(FrostConditionRuntime.Settings)).GetMethod);
        }

        public void EmitLoadCoreModuleSettings()
        {
            EmitLoadRuntime();
            Il.Emit(OpCodes.Callvirt,
                typeof(FrostConditionRuntime).GetProperty(nameof(FrostConditionRuntime.CoreModuleSettings)).GetMethod);
        }
    }
}
