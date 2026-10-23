using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;
using Celeste.Mods.FrostHelper.Runtime.Expressions;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Conditions;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Compilation;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Descriptors;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Contexts;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Commands.Binding;
using Session = Celeste.Simulation.Runtime.Session.ChapterCompletionSession;
using Entity = Celeste.Scene.Entities.SceneEntityState;
using Player = Celeste.Simulation.RoomPlayerSession;
using EntityID = Celeste.Core.Maps.MapEntityId;
using PackedColor = Celeste.Core.Display.PackedColor;

namespace Celeste.Mods.FrostHelper.Runtime.Expressions.Commands
{
    internal sealed class EntitySidAccessor : FieldAccessorCommand
    {
        private readonly FrostConditionRuntime runtime;

        public EntitySidAccessor(FrostConditionRuntime runtime, string name, string description)
        {
            this.runtime = runtime;
            Descriptor = new CommandDescriptor
            {
                Name = name, DeclaringType = TypeDescriptor.For(typeof(Entity)),
                ReturnType = TypeDescriptor.For(typeof(string)), Description = [RenderPart.Default(description)]
            };
        }

        public static string GetSid(Entity? entity, FrostConditionRuntime runtime) => entity is null
            ? "?"
            : runtime.EntityTypeNames.TypeToEntityName(entity.GetType()) ?? "?";

        public override object GetValue(object? obj) => GetSid((Entity?)obj, runtime);
        public override Type ReturnType => typeof(string);

        public override void Emit(ConditionCompilationCtx ctx, Type? knownInputType, Type targetType)
        {
            ctx.EmitLoadRuntime();
            ctx.Il.Emit(OpCodes.Call, typeof(EntitySidAccessor).GetMethod(nameof(GetSid))!);
            ctx.EmitConvertTo(typeof(string), targetType);
        }
    }
}
