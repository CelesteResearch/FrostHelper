using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Mods.FrostHelper.Entities.Collectibles.Feathers;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Collision.Responses;
using Celeste.Simulation.Gameplay.Player.Presentation.Animation;
using Celeste.Simulation.Gameplay.Player.Presentation.Hair;
using Celeste.Simulation.Gameplay.Player.Presentation.Sprites;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.FrostHelper.Runtime.Feathers
{
    internal sealed class FrostFeatherHooks : IDisposable
    {
        private readonly FrostFeatherRuntime runtime;
        private readonly List<IDisposable> hooks = new();
        private const BindingFlags Instance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        internal FrostFeatherHooks(FrostFeatherRuntime runtime)
        {
            this.runtime = runtime;
            try
            {
                hooks.Add(new ILHook(typeof(RoomPlayerSession).GetConstructors().Single(), RegisterPlayer));
                foreach (var name in new[] { "BeforeDownTransition", "BeforeUpTransition", "Render", "HiccupJump" })
                    hooks.Add(new ILHook(typeof(RoomPlayerSession).GetMethod(name, Instance),
                        il => ReplaceComparisons(il, LoadPlayer)));
                foreach (var name in new[] { "Horizontal", "Vertical", "RedDashCollision" })
                    hooks.Add(new ILHook(typeof(PlayerCollisionResponse).GetMethod(name, Instance),
                        il => ReplaceComparisons(il, LoadCollisionPlayer)));
                hooks.Add(new ILHook(typeof(PlayerHairAppearance).GetMethod("UpdateColor"),
                    il => ReplaceComparisons(il, cursor => LoadComponentPlayer(cursor, 4), 2)));
                hooks.Add(new ILHook(typeof(PlayerHairAppearance).GetMethod("UpdatePhysics"),
                    il => ReplaceComparisons(il, cursor => LoadComponentPlayer(cursor, 3), 2)));
                hooks.Add(new ILHook(typeof(PlayerAnimation).GetMethod("Update"),
                    il => ReplaceComparisons(il, cursor => LoadComponentPlayer(cursor, 2), 1)));
                hooks.Add(new Hook(typeof(PlayerSprite).GetMethod("Render"),
                    (Action<Action<PlayerSprite, ISourceRenderContext>, PlayerSprite, ISourceRenderContext>)
                    RenderSprite));
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private void RegisterPlayer(ILContext il)
        {
            var cursor = new ILCursor(il);
            while (cursor.TryGotoNext(MoveType.Before, instruction => instruction.OpCode == OpCodes.Ret))
            {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate<Action<RoomPlayerSession>>(player =>
                {
                    if (ReferenceEquals(player.EngineContext, runtime.Engine)) runtime.ForPlayer(player);
                });
                cursor.Index++;
            }
        }

        private static void LoadPlayer(ILCursor cursor) => cursor.Emit(OpCodes.Ldarg_0);

        private static void LoadCollisionPlayer(ILCursor cursor)
        {
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Callvirt, typeof(PlayerCollisionResponse).GetProperty("Player").GetMethod);
        }

        private static void LoadComponentPlayer(ILCursor cursor, int argument)
        {
            cursor.Emit(OpCodes.Ldarg, argument);
            cursor.EmitDelegate<Func<object, RoomPlayerSession>>(component =>
                ((Celeste.Core.Components.SourceComponentState)component).Entity as RoomPlayerSession);
        }

        private void ReplaceComparisons(ILContext il, Action<ILCursor> loadPlayer, int stateArgument = -1)
        {
            var cursor = new ILCursor(il);
            while (cursor.TryGotoNext(MoveType.After, instruction =>
                       (instruction.MatchLdcI4(19) || instruction.MatchLdcI4(5)) &&
                       (instruction.Previous.MatchCallvirt<SimulationStateMachine>("get_State") ||
                        stateArgument >= 0 && instruction.Previous.MatchLdarg(stateArgument))))
            {
                loadPlayer(cursor);
                cursor.EmitDelegate<Func<int, RoomPlayerSession, int>>(runtime.Replacement);
            }
        }

        private void RenderSprite(Action<PlayerSprite, ISourceRenderContext> original, PlayerSprite sprite,
            ISourceRenderContext context)
        {
            var player = sprite.Entity as RoomPlayerSession;
            if (player != null && ReferenceEquals(player.EngineContext, runtime.Engine) &&
                sprite.Color == Celeste.Core.Display.PackedColor.White &&
                runtime.TryGetPlayer(player, out var state) && player.Machine.State == state.StateId)
            {
                var color = sprite.Color;
                var hair = player.Components.Get<PlayerHair>();
                sprite.Color = hair?.Color ?? sprite.Color;
                original(sprite, context);
                sprite.Color = color;
            }
            else original(sprite, context);
        }

        public void Dispose()
        {
            for (var index = hooks.Count - 1; index >= 0; index--) hooks[index].Dispose();
            hooks.Clear();
        }
    }
}
