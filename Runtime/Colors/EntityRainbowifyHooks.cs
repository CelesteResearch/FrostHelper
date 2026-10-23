// SPDX-License-Identifier: MIT

using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Celeste.Core.Display;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.FrostHelper.Components.Colors;
using Celeste.Mods.FrostHelper.Entities.Controllers.Colors;
using Celeste.Mods.FrostHelper.Modules;
using Celeste.Scene.Entities;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.FrostHelper.Runtime.Colors
{
    public sealed class EntityRainbowifyHooks : IDisposable
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, EntityRainbowifyHooks> Owners = new();
        private readonly ISourceEngineContext engine;
        private readonly FrostHelperModule module;
        private readonly ILHook renderHook;
        private Hook addedHook;

        private EntityRainbowifyHooks(ISourceEngineContext engine, FrostHelperModule module)
        {
            this.engine = engine;
            this.module = module;
            renderHook =
                new ILHook(
                    typeof(OriginalLevelSceneRoot).GetMethod(nameof(OriginalLevelSceneRoot.Render), Type.EmptyTypes),
                    PatchLevelRender);
        }

        public static EntityRainbowifyHooks For(ISourceEngineContext engine, FrostHelperModule module) =>
            Owners.GetValue(engine, _ => new(engine, module));

        public static void Release(ISourceEngineContext engine)
        {
            if (Owners.TryGetValue(engine, out var hooks))
            {
                hooks.Dispose();
                Owners.Remove(engine);
            }
        }

        public PackedColor GetHue(object scene, Vector2 position) => module.Spinners.GetHue(scene, position);

        public void EnsureAddedHook() => addedHook ??=
            new Hook(typeof(SceneEntityState).GetMethod(nameof(SceneEntityState.Added)),
                (Action<Action<SceneEntityState, object>, SceneEntityState, object>)BeforeAdded);

        private void BeforeAdded(Action<SceneEntityState, object> original, SceneEntityState entity, object scene)
        {
            if (scene is OriginalLevelSceneRoot level && ReferenceEquals(level.EngineContext, engine))
            {
                var scheduler = level.SourceScene;
                var key = scheduler.Catalog.Find(typeof(AnyEntityAddedHook)).RegistryKey;
                if (scheduler.Components.Components.TryGetValue(key, out var callbacks))
                    foreach (AnyEntityAddedHook callback in callbacks)
                        callback.OnEntityAdded(entity, scene);
            }

            original(entity, scene);
        }

        private void PatchLevelRender(ILContext il)
        {
            var c = new ILCursor(il);
            if (!c.TryGotoNext(MoveType.After, i => i.MatchCall<PackedColor>("get_White")))
                throw new InvalidOperationException("Missing original Level.Render white tint anchor.");
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<PackedColor, OriginalLevelSceneRoot, PackedColor>>((white, level) =>
            {
                if (!ReferenceEquals(level.EngineContext, engine)) return white;
                var key = level.SourceScene.Catalog.Find(typeof(EntityRainbowifyController)).RegistryKey;
                var controllers = level.SourceScene.Entities.GetTrackedEntities(key);
                return controllers.Count > 0 && ((EntityRainbowifyController)controllers[0]).All
                    ? GetHue(engine.Frame.Scenes.CurrentScene, Vector2.Zero)
                    : white;
            });
        }

        public void Dispose()
        {
            addedHook?.Dispose();
            addedHook = null;
            renderHook.Dispose();
        }
    }
}
