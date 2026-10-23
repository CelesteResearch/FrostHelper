using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using Celeste.Core.Scheduling;
using Celeste.Game.Entities.Lightning;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.FrostHelper.Entities.Lighting;
using Celeste.Scene.Tracking;
using Celeste.Simulation.Gameplay.Player.Intro;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.FrostHelper.Runtime.Lighting
{
    public sealed class FrostArbitraryLightningHooks : IDisposable
    {
        private readonly ISourceEngineContext engine;
        private readonly List<IDisposable> hooks = new();
        private const BindingFlags StaticPrivate = BindingFlags.Static | BindingFlags.NonPublic;

        public FrostArbitraryLightningHooks(ISourceEngineContext engine)
        {
            this.engine = engine;
            try
            {
                hooks.Add(new Hook(typeof(OriginalLightningSequence).GetMethod("SetBreakValue", StaticPrivate),
                    (Action<Action<OriginalLevelSceneRoot, float>, OriginalLevelSceneRoot, float>)SetBreakValue));
                hooks.Add(new Hook(typeof(OriginalLightningSequence).GetMethod("SetPulseValue", StaticPrivate),
                    (Action<Action<OriginalLevelSceneRoot, float>, OriginalLevelSceneRoot, float>)SetPulseValue));
                hooks.Add(new Hook(typeof(OriginalLevelSceneRoot).GetMethod(nameof(OriginalLevelSceneRoot.LoadLevel)),
                    (Action<Action<OriginalLevelSceneRoot, PlayerIntroType, bool>, OriginalLevelSceneRoot,
                        PlayerIntroType, bool>)LoadLevel));
                var iterator = typeof(OriginalLightningSequence)
                    .GetMethod(nameof(OriginalLightningSequence.RemoveRoutine))
                    .GetCustomAttribute<IteratorStateMachineAttribute>().StateMachineType;
                var capturedLevel = iterator.GetField("level",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (capturedLevel == null || capturedLevel.FieldType != typeof(OriginalLevelSceneRoot))
                    throw new InvalidOperationException("Lightning.RemoveRoutine level capture is missing.");
                hooks.Add(new ILHook(iterator.GetMethod("MoveNext", BindingFlags.NonPublic | BindingFlags.Instance),
                    il => PatchRemoveRoutine(il, capturedLevel)));
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private bool Owns(OriginalLevelSceneRoot level) => ReferenceEquals(level.EngineContext, engine);

        private void SetBreakValue(Action<OriginalLevelSceneRoot, float> orig, OriginalLevelSceneRoot level,
            float value)
        {
            orig(level, value);
            if (!Owns(level)) return;
            if (level.SourceScene.Entities.TryGetTrackedEntities(typeof(CustomLightningRenderer), out var renderers))
                foreach (CustomLightningRenderer renderer in renderers)
                    if (renderer.AffectedByLightningBoxes)
                        renderer.Fade = value * .6f;
        }

        private void SetPulseValue(Action<OriginalLevelSceneRoot, float> orig, OriginalLevelSceneRoot level,
            float value)
        {
            orig(level, value);
            if (!Owns(level)) return;
            if (level.SourceScene.Entities.TryGetTrackedEntities(typeof(CustomLightningRenderer), out var renderers))
                foreach (CustomLightningRenderer renderer in renderers)
                    if (renderer.AffectedByLightningBoxes)
                        renderer.Fade = value * .2f;
        }

        private void LoadLevel(Action<OriginalLevelSceneRoot, PlayerIntroType, bool> orig, OriginalLevelSceneRoot level,
            PlayerIntroType intro, bool fromLoader)
        {
            if (Owns(level) &&
                level.SourceScene.Entities.TryGetTrackedEntities(typeof(CustomLightningRenderer), out var renderers))
                foreach (CustomLightningRenderer renderer in renderers)
                    renderer.Reset();
            orig(level, intro, fromLoader);
        }

        private void MakeUncollidable(OriginalLevelSceneRoot level)
        {
            if (!Owns(level)) return;
            level = (OriginalLevelSceneRoot)engine.Frame.Scenes.CurrentScene;
            if (level.SourceScene.Entities.TryGetTrackedEntities(typeof(ArbitraryShapeLightning), out var entities))
                foreach (ArbitraryShapeLightning entity in entities)
                    if (entity.AffectedByBreakerBoxes)
                        entity.Collidable = false;
            if (level.SourceScene.Entities.TryGetTrackedEntities(typeof(CustomLightningRenderer), out var renderers))
                foreach (CustomLightningRenderer renderer in renderers)
                    if (renderer.AffectedByLightningBoxes)
                        renderer.UpdateSeeds = false;
        }

        private void Shatter(OriginalLevelSceneRoot level)
        {
            if (!Owns(level)) return;
            level = (OriginalLevelSceneRoot)engine.Frame.Scenes.CurrentScene;
            if (level.SourceScene.Entities.TryGetTrackedEntities(typeof(ArbitraryShapeLightning), out var entities))
                foreach (ArbitraryShapeLightning entity in entities)
                    if (entity.AffectedByBreakerBoxes)
                        entity.Shatter();
        }

        private void Remove(OriginalLevelSceneRoot level)
        {
            if (!Owns(level)) return;
            level = (OriginalLevelSceneRoot)engine.Frame.Scenes.CurrentScene;
            if (level.SourceScene.Entities.TryGetTrackedEntities(typeof(ArbitraryShapeLightning), out var entities))
                foreach (ArbitraryShapeLightning entity in entities)
                    if (entity.AffectedByBreakerBoxes)
                        entity.RemoveSelf();
        }

        private void PatchRemoveRoutine(ILContext il, FieldInfo level)
        {
            // Original injects at MoveNext entry, so this executes on every coroutine resume.
            var cursor = new ILCursor(il);

            void Emit(Action<OriginalLevelSceneRoot> action)
            {
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldfld, level);
                cursor.EmitDelegate(action);
            }

            Emit(MakeUncollidable);
            if (!cursor.TryGotoNext(MoveType.After,
                    i => i.MatchCallOrCallvirt<OriginalLevelSceneRoot>(nameof(OriginalLevelSceneRoot.Shake))))
                throw new InvalidOperationException("Lightning.RemoveRoutine Shake call is missing.");
            Emit(Shatter);
            if (!cursor.TryGotoNext(MoveType.After, i => i.MatchLdcR4(0f),
                    i => i.MatchCall(typeof(OriginalLightningSequence).GetMethod("SetBreakValue", StaticPrivate))))
                throw new InvalidOperationException("Lightning.RemoveRoutine final break reset is missing.");
            Emit(Remove);
        }

        public void Dispose()
        {
            for (int i = hooks.Count - 1; i >= 0; i--) hooks[i].Dispose();
            hooks.Clear();
        }
    }
}
