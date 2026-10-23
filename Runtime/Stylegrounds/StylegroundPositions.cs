using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Numerics;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Levels;
using Celeste.Simulation.Gameplay.Player.Intro;
using Celeste.Simulation.Presentation.Backdrops.Runtime;
using Celeste.Simulation.Presentation.Backdrops.Rendering;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.FrostHelper.Runtime.Stylegrounds
{
    public sealed class StylegroundPositions : IDisposable
    {
        private sealed class OriginalPosition
        {
            internal Vector2? Value;
        }

        private static readonly ConditionalWeakTable<ISourceEngineContext, StylegroundPositions> owners = new();
        private readonly ISourceEngineContext engine;
        private readonly ConditionalWeakTable<BackdropPresentation, OriginalPosition> positions = new();
        private Hook loadLevel;

        private delegate void OriginalLoad(OriginalLevelSceneRoot level, PlayerIntroType intro, bool isFromLoader);

        private StylegroundPositions(ISourceEngineContext engine) => this.engine = engine;

        public static StylegroundPositions For(ISourceEngineContext engine) => owners.GetValue(engine,
            current => new StylegroundPositions(current));

        public void Load() => loadLevel = new Hook(typeof(OriginalLevelSceneRoot).GetMethod(
                nameof(OriginalLevelSceneRoot.LoadLevel), new[] { typeof(PlayerIntroType), typeof(bool) }),
            (Action<OriginalLoad, OriginalLevelSceneRoot, PlayerIntroType, bool>)AfterLoad);

        private void AfterLoad(OriginalLoad original, OriginalLevelSceneRoot level, PlayerIntroType intro,
            bool isFromLoader)
        {
            original(level, intro, isFromLoader);
            if (ReferenceEquals(level.EngineContext, engine)) ResetPositions(level);
        }

        public void StoreOriginal(BackdropPresentation backdrop)
        {
            var state = positions.GetOrCreateValue(backdrop);
            state.Value ??= backdrop.Position;
        }

        public void StoreEnd(BackdropPresentation backdrop, Vector2 value) =>
            positions.GetOrCreateValue(backdrop).Value = value;

        public void ResetPositions(OriginalLevelSceneRoot level)
        {
            Reset(level.Background);
            Reset(level.Foreground);
        }

        private void Reset(BackdropRendererPresentation renderer)
        {
            foreach (var backdrop in renderer.Backdrops)
            {
                var value = positions.GetOrCreateValue(backdrop).Value;
                if (value.HasValue) backdrop.Position = value.GetValueOrDefault();
            }
        }

        public void Dispose()
        {
            loadLevel?.Dispose();
            loadLevel = null;
        }
    }
}
