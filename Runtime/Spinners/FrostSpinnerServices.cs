// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Celeste.Core.Randomness;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.FrostHelper.Content.Spinners;

namespace Celeste.Mods.FrostHelper.Runtime.Spinners
{
    public sealed class FrostSpinnerServices
    {
        private readonly ISourceEngineContext engine;
        private readonly Dictionary<(string, string, bool), SpinnerSpriteSource> spriteSources = new();
        private readonly ConditionalWeakTable<OriginalLevelSceneRoot, SpinnerSceneGroups> scenes = new();
        private Celeste.Game.Entities.Hazards.Spinners.Crystal.OriginalCrystalStaticSpinner hueSource;

        public FrostSpinnerServices(ISourceEngineContext engine) => this.engine = engine;

        public SpinnerSceneGroups ForScene(OriginalLevelSceneRoot scene)
            => scenes.GetValue(scene, static level => new SpinnerSceneGroups(level));

        public SpinnerSpriteSource GetSprites(string directory, string suffix, bool animated = false)
        {
            var key = (directory, suffix, animated);
            if (!spriteSources.TryGetValue(key, out var value))
                spriteSources.Add(key, value = new SpinnerSpriteSource(engine, directory, suffix, animated));
            return value;
        }

        public void InitializeHue()
        {
            if (hueSource != null) return;
            // Original ColorHelper reuses one detached vanilla spinner, including its source collider and RNG.
            hueSource = new Celeste.Game.Entities.Hazards.Spinners.Crystal.OriginalCrystalStaticSpinner(
                System.Numerics.Vector2.Zero, false,
                Celeste.Simulation.Gameplay.Hazards.Spinners.Crystal.CrystalSpinnerColor.Rainbow, engine);
        }

        public void SetHueScene(object scene)
        {
            InitializeHue();
            ((Celeste.Scene.Tracking.ISceneEntityAssociation)hueSource).Scene = scene;
        }

        public Celeste.Core.Display.PackedColor GetHue(System.Numerics.Vector2 position) => hueSource.GetHue(position);

        public Celeste.Core.Display.PackedColor GetHue(object scene, System.Numerics.Vector2 position)
        {
            SetHueScene(scene);
            return GetHue(position);
        }
    }
}
