// SPDX-License-Identifier: MIT

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Celeste.Core.Display;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Gameplay.Effects.Debris;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Display;
using Celeste.Graphics.Content.Tiles;
using Celeste.Mods.FrostHelper.Entities.Controllers.Colors;
using Celeste.Mods.FrostHelper.Runtime.Spinners;
using Celeste.Scene.Entities;
using Celeste.Scene.Scheduling;
using Celeste.Scene.Tracking;
using Celeste.Simulation.Gameplay.Actors.Debris;
using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.FrostHelper.Runtime.Colors
{
    public sealed class RainbowTilesetHooks : IDisposable
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, RainbowTilesetHooks> Owners = new();
        private readonly ISourceEngineContext engine;
        private readonly FrostSpinnerServices services;
        private readonly ISourceRenderEnvironment renderEnvironment;
        private readonly ILHook renderHook;
        private readonly Hook debrisHook;
        private readonly Action<SceneEntityState> afterDebrisUpdate;

        private RainbowTilesetHooks(ISourceEngineContext engine, FrostSpinnerServices services)
        {
            this.engine = engine;
            this.services = services;
            renderEnvironment = (ISourceRenderEnvironment)engine;
            afterDebrisUpdate = AfterDebrisUpdate;
            renderHook = new ILHook(typeof(TileGrid).GetMethod(nameof(TileGrid.RenderAt)), PatchRender);
            try
            {
                debrisHook = new Hook(typeof(EverestDebrisState).GetMethod(nameof(EverestDebrisState.Init),
                        new[] { typeof(Vector2), typeof(char), typeof(bool) }),
                    (Func<Func<EverestDebrisState, Vector2, char, bool, DebrisState>, EverestDebrisState, Vector2, char,
                        bool, DebrisState>)InitDebris);
            }
            catch
            {
                renderHook.Dispose();
                throw;
            }
        }

        public static RainbowTilesetHooks For(ISourceEngineContext engine, FrostSpinnerServices services) =>
            Owners.GetValue(engine, _ => new(engine, services));

        public static void Release(ISourceEngineContext engine)
        {
            if (!Owners.TryGetValue(engine, out var hooks)) return;
            hooks.Dispose();
            Owners.Remove(engine);
        }

        public PackedColor GetHue(Vector2 position) => services.GetHue(position);

        private RainbowTilesetController GetController()
        {
            object scene = engine.Frame.Scenes.CurrentScene;
            services.SetHueScene(scene);
            var tracker = ((ISourceScene)scene).SourceScene.Entities;
            return tracker.TryGetTrackedEntities(typeof(RainbowTilesetController), out var controllers) &&
                   controllers.Count > 0
                ? (RainbowTilesetController)controllers[0]
                : null;
        }

        private RainbowTilesetController GetController(ISourceRenderContext context) =>
            ReferenceEquals(context, renderEnvironment.CurrentContext) ? GetController() : null;

        private DebrisState InitDebris(Func<EverestDebrisState, Vector2, char, bool, DebrisState> original,
            EverestDebrisState self, Vector2 position, char tileset, bool playSound)
        {
            DebrisState result = original(self, position, tileset, playSound);
            if (!ReferenceEquals(self.ActorEngineContext, engine)) return result;
            var controller = GetController();
            if (controller != null && controller.IncludesDebris(tileset)) result.PostUpdate += afterDebrisUpdate;
            return result;
        }

        private void AfterDebrisUpdate(SceneEntityState entity)
        {
            var debris = (DebrisState)entity;
            debris.DebrisImage.Color =
                services.GetHue(debris.Scene, debris.DebrisImage.RenderPosition) * debris.DebrisAlpha;
        }

        private void PatchRender(ILContext il)
        {
            var cursor = new ILCursor(il);
            var controller = new VariableDefinition(il.Import(typeof(RainbowTilesetController)));
            il.Body.Variables.Add(controller);
            cursor.Emit(OpCodes.Ldarg_2);
            cursor.EmitDelegate<Func<ISourceRenderContext, RainbowTilesetController>>(GetController);
            cursor.Emit(OpCodes.Stloc, controller);
            if (!cursor.TryGotoNext(MoveType.Before,
                    instruction => (instruction.OpCode == OpCodes.Newobj || instruction.OpCode == OpCodes.Call) &&
                                   instruction.Operand is MethodReference method && method.Name == ".ctor" &&
                                   method.DeclaringType.FullName == typeof(AtlasTextureDraw).FullName))
                throw new InvalidOperationException("Missing original TileGrid AtlasTextureDraw constructor.");
            // The constructor receives texture, drawPosition, origin, color, scale and optional flags. Insert before scale
            // so the original color remains on the stack and no draw command or texture metadata is recreated.
            if (!cursor.TryGotoPrev(MoveType.Before, instruction => instruction.MatchCall<Vector2>("get_One")))
                throw new InvalidOperationException("Missing original TileGrid unit scale argument.");
            int scaleLoad = cursor.Index;
            int colorLoad = scaleLoad - 1;
            if (!il.Body.Instructions[colorLoad].MatchLdloc(out int color) ||
                !il.Body.Instructions[colorLoad - 2].MatchLdloc(out int position) ||
                !il.Body.Instructions[colorLoad - 3].MatchLdloc(out int texture))
                throw new InvalidOperationException("Missing original TileGrid texture/position/color argument order.");
            cursor.Index = scaleLoad;
            cursor.Emit(OpCodes.Ldloc, position);
            cursor.Emit(OpCodes.Ldloc, texture);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldloc, controller);
            cursor
                .EmitDelegate<
                    Func<PackedColor, Vector2, AtlasEntry, TileGrid, RainbowTilesetController, PackedColor>>(static (
                        original, at, tile, grid, owner) =>
                    owner == null ? original : owner.GetColor(original, at, tile, grid));
        }

        public void Dispose()
        {
            debrisHook.Dispose();
            renderHook.Dispose();
        }
    }
}
