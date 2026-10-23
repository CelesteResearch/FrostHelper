using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Numerics;
using Celeste.Mods.FrostHelper.Runtime.Expressions;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Conditions;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Compilation;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Descriptors;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Contexts;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Commands.Binding;
using Celeste.Scene.Tracking;
using Session = Celeste.Simulation.Runtime.Session.ChapterCompletionSession;
using EntityID = Celeste.Core.Maps.MapEntityId;
using Player = Celeste.Simulation.RoomPlayerSession;
using Settings = Celeste.Game.Settings.SettingsState;
using CoreModuleSettings = Celeste.Everest.EverestCoreSettings;

namespace Celeste.Mods.FrostHelper.Runtime.Expressions.Commands
{
    internal sealed partial class SimpleCommands
    {
        private sealed class DeltaTimeAccessor(FrostConditionRuntime runtime) : Condition
        {
            public override object Get(Session session, object? userdata) => runtime.Engine.DeltaTime;

            protected internal override Type ReturnType => typeof(float);

            internal override void Emit(ConditionCompilationCtx ctx, Type targetType)
            {
                var il = ctx.Il;
                ctx.EmitLoadRuntime();
                il.Emit(OpCodes.Callvirt,
                    typeof(FrostConditionRuntime).GetProperty(nameof(FrostConditionRuntime.Engine))!.GetMethod!);
                il.Emit(OpCodes.Callvirt,
                    typeof(Celeste.Core.Scheduling.ISourceEngineContext).GetProperty("DeltaTime")!.GetMethod!);
                il.EmitConvertToInSessionExpression(typeof(float), targetType);
            }

            internal override bool UsesCurrentConditionLocalInEmit => false;
        }

        private sealed class TimeAccessor(FrostConditionRuntime runtime) : Condition
        {
            public override object Get(Session session, object? userdata) =>
                ((ISourceScene)runtime.CurrentScene).SourceScene.Clock.TimeActive;

            protected internal override Type ReturnType => typeof(float);

            internal override void Emit(ConditionCompilationCtx ctx, Type targetType)
            {
                var il = ctx.Il;
                ctx.EmitLoadScene();
                il.Emit(OpCodes.Castclass, typeof(ISourceScene));
                il.Emit(OpCodes.Callvirt, typeof(ISourceScene).GetProperty("SourceScene")!.GetMethod!);
                il.Emit(OpCodes.Callvirt,
                    typeof(Celeste.Scene.Scheduling.SceneUpdateScheduler).GetProperty("Clock")!.GetMethod!);
                il.Emit(OpCodes.Ldfld, typeof(Celeste.Scene.Scheduling.SceneClock).GetField("TimeActive")!);
                il.EmitConvertToInSessionExpression(typeof(float), targetType);
            }

            internal override bool UsesCurrentConditionLocalInEmit => false;
        }

        private sealed class SessionFieldAccessor(string fieldName) : Condition
        {
            private readonly Func<object, object> getter = ColdObjectGetter.ForField(typeof(Session).GetField(fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public)!);

            public readonly FieldInfo Field = typeof(Session).GetField(fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public)!;

            public override object Get(Session session, object? userdata)
            {
                var ret = getter(session)!;
                return ret is bool b ? BoolToBoxedInt(b) : ret;
            }

            internal override void Emit(ConditionCompilationCtx ctx, Type targetType)
            {
                ctx.EmitLoadSession();
                ctx.Il.Emit(OpCodes.Ldfld, Field);
                ctx.EmitConvertTo(ReturnType, targetType);
            }

            internal override bool UsesCurrentConditionLocalInEmit => false;

            protected internal override Type ReturnType => Field.FieldType;
        }

        private sealed class SettingsFieldAccessor(FrostConditionRuntime runtime, string fieldName) : Condition
        {
            private readonly Func<object, object> getter = ColdObjectGetter.ForField(
                typeof(Settings).GetField(fieldName,
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public)!);

            public readonly FieldInfo Field = typeof(Settings).GetField(fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public)!;

            public override object Get(Session session, object? userdata)
            {
                var ret = getter(runtime.Settings)!;
                return ret is bool b ? BoolToBoxedInt(b) : ret;
            }

            internal override void Emit(ConditionCompilationCtx ctx, Type targetType)
            {
                ctx.EmitLoadSettings();
                ctx.Il.Emit(OpCodes.Ldfld, Field);
                ctx.EmitConvertTo(ReturnType, targetType);
            }

            internal override bool UsesCurrentConditionLocalInEmit => false;

            protected internal override Type ReturnType => Field.FieldType;
        }

        private sealed class CoreModuleSettingsPropertyAccessor(FrostConditionRuntime runtime, string fieldName)
            : Condition
        {
            private readonly Func<object, object> getter = ColdObjectGetter.ForProperty(
                typeof(CoreModuleSettings).GetProperty(fieldName,
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public)!);

            public readonly PropertyInfo Property = typeof(CoreModuleSettings).GetProperty(fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public)!;

            public override object Get(Session session, object? userdata)
            {
                var ret = getter(runtime.CoreModuleSettings)!;
                return ret is bool b ? BoolToBoxedInt(b) : ret;
            }

            internal override void Emit(ConditionCompilationCtx ctx, Type targetType)
            {
                ctx.EmitLoadCoreModuleSettings();
                ctx.Il.Emit(OpCodes.Call, Property.GetMethod!);
                ctx.EmitConvertTo(ReturnType, targetType);
            }

            internal override bool UsesCurrentConditionLocalInEmit => false;

            protected internal override Type ReturnType => Property.PropertyType;
        }

        private sealed class CoreModeAccessor : Condition
        {
            public override object Get(Session session, object? userdata)
            {
                return (int)session.CoreMode;
            }

            public override bool OnlyChecksFlags() => false;

            protected internal override Type ReturnType => typeof(int);
        }

        private sealed class DashAccessor(FrostConditionRuntime runtime) : PlayerGetterCondition<int>(runtime)
        {
            protected override int GetFromPlayer(Player player) => player.Data.Dashes;
        }

        private sealed class MaxDashAccessor(FrostConditionRuntime runtime) : PlayerGetterCondition<int>(runtime)
        {
            protected override int GetFromPlayer(Player player) => player.Motor.BeforeState.MaxDashes;
        }

        private sealed class PlayerAccessor(FrostConditionRuntime runtime) : PlayerGetterCondition<Player>(runtime)
        {
            protected override Player GetFromPlayer(Player player) => player;
        }

        private sealed class PlayerSpeedAccessor(FrostConditionRuntime runtime)
            : PlayerGetterCondition<Vector2>(runtime)
        {
            protected override Vector2 GetFromPlayer(Player player) => player.Speed;
        }

        private sealed class StaminaAccessor(FrostConditionRuntime runtime) : PlayerGetterCondition<float>(runtime)
        {
            protected override float GetFromPlayer(Player player) => player.Data.Stamina;
        }

        private abstract class PlayerGetterCondition<T> : Condition where T : notnull
        {
            private readonly FrostConditionRuntime runtime;
            private readonly string playerKey;
            private T lastValue;
            private bool hasLastValue;

            protected PlayerGetterCondition(FrostConditionRuntime runtime)
            {
                this.runtime = runtime;
                playerKey = ((ISourceSceneCatalogEnvironment)runtime.Engine).SceneCatalog.Find(typeof(Player))
                    .RegistryKey;
            }

            protected abstract T GetFromPlayer(Player player);

            private Player FindPlayer()
            {
                var entities = ((ISourceScene)runtime.CurrentScene).SourceScene.Entities;
                return entities.IsTracked(playerKey) ? entities.GetTrackedEntity<Player>(playerKey) : null;
            }

            private T GetTyped()
            {
                if (FindPlayer() is { } player)
                {
                    hasLastValue = true;
                    return lastValue = GetFromPlayer(player);
                }

                return hasLastValue ? lastValue : Coerce<T>(Zero, runtime);
            }

            public override object Get(Session session, object? userdata)
            {
                if (FindPlayer() is { } player)
                {
                    hasLastValue = true;
                    return lastValue = GetFromPlayer(player);
                }

                return hasLastValue ? lastValue : Zero;
            }

            internal override void Emit(ConditionCompilationCtx ctx, Type targetType)
            {
                ctx.EmitLoadCurrentCondition<PlayerGetterCondition<T>>();
                ctx.Il.Emit(OpCodes.Call,
                    typeof(PlayerGetterCondition<T>).GetMethod(nameof(GetTyped),
                        BindingFlags.NonPublic | BindingFlags.Instance)!);
                ctx.EmitConvertTo(typeof(T), targetType);
            }

            public override bool OnlyChecksFlags() => false;
            protected internal override Type ReturnType => typeof(T);
        }
    }
}
