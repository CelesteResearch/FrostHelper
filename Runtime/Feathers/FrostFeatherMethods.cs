using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using Celeste.Core.Display;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.FrostHelper.Entities.Collectibles.Feathers;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Scene.Particles;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.Utils;

namespace Celeste.Mods.FrostHelper.Runtime.Feathers
{
    internal sealed class FrostFeatherMethods : IDisposable
    {
        private readonly List<DynamicMethodDefinition> definitions = new();
        internal readonly Action<RoomPlayerSession, CustomFeather> End;

        internal readonly Func<RoomPlayerSession, CustomFeather, int, OriginalLevelSceneRoot,
            Celeste.Simulation.Presentation.PostProcessing.Bloom.BloomPointState, int> Update;

        internal FrostFeatherMethods()
        {
            try
            {
                var end = Clone("EndStarFly", typeof(CustomFeather));
                using (var il = new ILContext(end.Definition))
                {
                    ReplaceParticleEmission(il, false);
                    var displacement = new ILCursor(il);
                    displacement.GotoNext(MoveType.Before,
                        instruction => instruction.Operand is MethodReference method &&
                                       method.Name == "StarFlyDisplacement");
                    displacement.Next.OpCode = OpCodes.Call;
                    displacement.Next.Operand = il.Method.Module.ImportReference(
                        typeof(FrostFeatherAccess).GetMethod(nameof(FrostFeatherAccess.Displace),
                            BindingFlags.Static | BindingFlags.NonPublic));
                    var cursor = new ILCursor(il);
                    cursor.GotoNext(MoveType.Before, instruction => instruction.OpCode == OpCodes.Ret);
                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.EmitDelegate<Action<RoomPlayerSession>>(SetDefaultColor);
                }

                End = (Action<RoomPlayerSession, CustomFeather>)end.Generate()
                    .CreateDelegate(typeof(Action<RoomPlayerSession, CustomFeather>));
                var update = Clone("UpdateStarFly", typeof(CustomFeather), typeof(int), typeof(OriginalLevelSceneRoot),
                    typeof(Celeste.Simulation.Presentation.PostProcessing.Bloom.BloomPointState));
                using (var il = new ILContext(update.Definition)) PatchUpdate(il);
                Update =
                    (Func<RoomPlayerSession, CustomFeather, int, OriginalLevelSceneRoot,
                        Celeste.Simulation.Presentation.PostProcessing.Bloom.BloomPointState, int>)update.Generate()
                        .CreateDelegate(
                            typeof(Func<RoomPlayerSession, CustomFeather, int, OriginalLevelSceneRoot,
                                Celeste.Simulation.Presentation.PostProcessing.Bloom.BloomPointState, int>));
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private DynamicMethodDefinition Clone(string name, params Type[] additionalParameters)
        {
            var definition =
                new DynamicMethodDefinition(
                    typeof(RoomPlayerSession).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic));
            foreach (var type in additionalParameters)
                definition.Definition.Parameters.Add(
                    new ParameterDefinition(definition.Definition.Module.ImportReference(type)));
            definitions.Add(definition);
            return definition;
        }

        private static readonly Action<RoomPlayerSession, PackedColor> SetColor =
            FrostFeatherAccess.FieldSetter<PackedColor>("starFlyColor");

        private static void SetDefaultColor(RoomPlayerSession player) =>
            SetColor(player, new PackedColor(255, 214, 92, 255));

        private static void EmitEnd(RoomPlayerSession player, CustomFeather feather) =>
            ((OriginalLevelSceneRoot)player.Scene).Particles.Emit(feather.P_Boost, 12, player.Center, Vector2.One * 4,
                Celeste.Core.Mathematics.Vector2Geometry.Angle(-player.Speed));

        private static void EmitFlying(RoomPlayerSession player, CustomFeather feather, OriginalLevelSceneRoot level) =>
            level.Particles.Emit(feather.P_Flying, 1, player.Center, Vector2.One * 2, feather.FlyColor,
                Celeste.Core.Mathematics.Vector2Geometry.Angle(-player.Speed));

        private static void ReplaceParticleEmission(ILContext il, bool flying)
        {
            var cursor = new ILCursor(il);
            cursor.GotoNext(MoveType.Before,
                instruction =>
                    instruction.MatchCallvirt<ISourcePlayerStarFlyScene>(nameof(ISourcePlayerStarFlyScene
                        .EmitStarFlyParticles)));
            var call = cursor.Next;
            cursor.GotoPrev(MoveType.Before,
                instruction => instruction.Operand is MethodReference method && method.Name == "get_Level");
            cursor.Index--;
            var first = cursor.Index;
            var last = il.Instrs.IndexOf(call);
            for (var index = first; index <= last; index++)
            {
                il.Instrs[index].OpCode = OpCodes.Nop;
                il.Instrs[index].Operand = null;
            }

            cursor.Index = last + 1;
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldarg_1);
            if (flying)
            {
                cursor.Emit(OpCodes.Ldarg_3);
                cursor.EmitDelegate<Action<RoomPlayerSession, CustomFeather, OriginalLevelSceneRoot>>(EmitFlying);
            }
            else cursor.EmitDelegate<Action<RoomPlayerSession, CustomFeather>>(EmitEnd);
        }

        private static void PatchUpdate(ILContext il)
        {
            var cursor = new ILCursor(il);
            var flyTime = new VariableDefinition(il.Method.Module.ImportReference(typeof(float)));
            il.Body.Variables.Add(flyTime);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.Emit(OpCodes.Ldfld, typeof(CustomFeather).GetField(nameof(CustomFeather.FlyTime)));
            cursor.Emit(OpCodes.Stloc, flyTime);
            while (cursor.TryGotoNext(MoveType.Before,
                       instruction =>
                           instruction.Operand is MethodReference method && method.Name == "get_StarFlyBloom"))
            {
                cursor.Next.OpCode = OpCodes.Nop;
                cursor.Next.Operand = null;
                cursor.Index++;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldarg, 4);
            }

            cursor.Index = 0;
            cursor.GotoNext(MoveType.After,
                instruction => instruction.OpCode == OpCodes.Stfld && instruction.Operand is FieldReference alpha &&
                               alpha.Name == "Alpha");
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldarg, 4);
            cursor.Emit(OpCodes.Call, typeof(RoomPlayerSession).GetProperty("StarFlyBloom").SetMethod);
            cursor.Index = 0;
            cursor.GotoNext(MoveType.After, instruction => instruction.MatchLdcR4(2));
            cursor.Emit(OpCodes.Pop);
            cursor.Emit(OpCodes.Ldloc, flyTime);
            cursor.Index = 0;
            cursor.GotoNext(MoveType.After,
                instruction => instruction.OpCode == OpCodes.Callvirt &&
                               instruction.Operand is MethodReference method && method.Name == "get_Value" &&
                               method.ReturnType.FullName == typeof(Vector2).FullName);
            cursor.Emit(OpCodes.Pop);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.Emit(OpCodes.Call,
                typeof(CustomFeather).GetMethod("GetAim", BindingFlags.Instance | BindingFlags.NonPublic));
            cursor.Index = 0;
            cursor.GotoNext(MoveType.After, instruction => instruction.MatchLdcR4(91));
            cursor.Emit(OpCodes.Pop);
            LoadField(cursor, nameof(CustomFeather.NeutralSpeed));
            cursor.Index = 0;
            var lowCount = 0;
            while (cursor.TryGotoNext(MoveType.After, instruction => instruction.MatchLdcR4(140)))
            {
                cursor.Emit(OpCodes.Pop);
                if (lowCount++ == 0) LoadField(cursor, nameof(CustomFeather.LowSpeed));
                else
                {
                    cursor.Emit(OpCodes.Ldarg_1);
                    cursor.Emit(OpCodes.Call,
                        typeof(CustomFeather).GetMethod("GetLowSpeedHardcodedIfOld",
                            BindingFlags.Instance | BindingFlags.NonPublic));
                }
            }

            if (lowCount != 3)
                throw new InvalidOperationException(
                    "CustomFeather expected three floating vanilla low-speed reads; actual=" + lowCount + ".");
            cursor.Index = 0;
            cursor.GotoNext(MoveType.After, instruction => instruction.MatchLdcI4(140));
            cursor.Emit(OpCodes.Pop);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.Emit(OpCodes.Call,
                typeof(CustomFeather).GetMethod("GetLowSpeedHardcodedIfOld",
                    BindingFlags.Instance | BindingFlags.NonPublic));
            cursor.GotoNext(MoveType.After,
                instruction => instruction.OpCode == OpCodes.Call && instruction.Operand is MethodReference sign &&
                               sign.DeclaringType.FullName == typeof(Math).FullName && sign.Name == "Sign");
            cursor.Emit(OpCodes.Conv_R4);
            cursor.Index = 0;
            cursor.GotoNext(MoveType.After, instruction => instruction.MatchLdcR4(190));
            cursor.Emit(OpCodes.Pop);
            LoadField(cursor, nameof(CustomFeather.MaxSpeed));
            ReplaceParticleEmission(il, true);
            cursor.Index = 0;
            while (cursor.TryGotoNext(MoveType.Before,
                       instruction => instruction.Operand is MethodReference method && method.Name == "get_Level"))
            {
                cursor.Next.OpCode = OpCodes.Nop;
                cursor.Next.Operand = null;
                cursor.Index++;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldarg_3);
            }

            cursor.Index = 0;
            cursor.GotoNext(MoveType.Before, instruction => instruction.MatchLdfld<RoomPlayerSession>("starFlyColor"));
            var firstColor = cursor.Next;
            cursor.GotoPrev(MoveType.Before,
                instruction => instruction.MatchLdfld<RoomPlayerSession>(nameof(RoomPlayerSession.Sprite)));
            cursor.Index--;
            var color = new VariableDefinition(il.Method.Module.ImportReference(typeof(PackedColor)));
            il.Body.Variables.Add(color);
            LoadField(cursor, nameof(CustomFeather.FlyColor));
            cursor.Emit(OpCodes.Stloc, color);
            cursor.Index = il.Instrs.IndexOf(firstColor);
            var colorReads = 0;
            do
            {
                cursor.Next.OpCode = OpCodes.Nop;
                cursor.Next.Operand = null;
                cursor.Index++;
                cursor.Emit(OpCodes.Pop);
                cursor.Emit(OpCodes.Ldloc, color);
                colorReads++;
            } while (cursor.TryGotoNext(MoveType.Before,
                         instruction => instruction.MatchLdfld<RoomPlayerSession>("starFlyColor")));

            if (colorReads != 2)
                throw new InvalidOperationException("CustomFeather expected two vanilla blink color reads.");
            cursor.Index = 0;
            cursor.GotoNext(MoveType.Before,
                instruction => instruction.MatchLdcI4(19) && instruction.Next.OpCode == OpCodes.Ret);
            cursor.Next.OpCode = OpCodes.Nop;
            cursor.Next.Operand = null;
            cursor.Index++;
            cursor.Emit(OpCodes.Ldarg_2);
        }

        private static void LoadField(ILCursor cursor, string name)
        {
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.Emit(OpCodes.Ldfld, typeof(CustomFeather).GetField(name));
        }

        public void Dispose()
        {
            foreach (var definition in definitions) definition.Dispose();
            definitions.Clear();
        }
    }
}
