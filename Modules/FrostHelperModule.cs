// SPDX-License-Identifier: MIT

using Celeste.Core.Scheduling;
using Celeste.Everest;
using Celeste.Everest.Diagnostics.Logging;
using Celeste.Everest.Modules.Runtime;
using Celeste.Mods.FrostHelper.Color;
using Celeste.Mods.FrostHelper.Runtime.Spinners;

namespace Celeste.Mods.FrostHelper.Modules
{
    public sealed partial class FrostHelperModule : SourceEverestModule
    {
        private System.IDisposable fastCrystalDebrisPool;
        private Rendering.Batches.EntityBatchRenderLifecycle entityBatchers;

        public ColorReader Colors { get; }
        internal FrostSpinnerServices Spinners { get; }

        internal void Warn(string message) => ((IEverestLoggerProcessEnvironment)SourceEngine).Logger.Log(
            EverestLogLevel.Error, "FrostHelper", message);

        public FrostHelperModule(ISourceEngineContext engine) : base(engine)
        {
            Colors = new ColorReader(Warn);
            Spinners = new FrostSpinnerServices(engine);
        }

        public override void Load()
        {
            Runtime.Expressions.FrostConditionRuntime.For(SourceEngine).Load();
            entityBatchers = new Rendering.Batches.EntityBatchRenderLifecycle(SourceEngine);
            Runtime.Movement.NoMovementHooks.Load(SourceEngine);
            LoadActivators();
            LoadLightningColors();
            EnsureBoosterHooks();
            Runtime.Feathers.FrostFeatherRuntime.For(SourceEngine).Load();
            LoadSprings();
            LoadDecalContainers();
            LoadStylegroundPositions();
            LoadFlashlightColors();
            LoadBloomColors();
            Runtime.Colors.EntityRainbowifyHooks.For(SourceEngine, this);
        }

        public override void Unload()
        {
            entityBatchers?.Dispose();
            entityBatchers = null;
            Runtime.Colors.RainbowTilesetHooks.Release(SourceEngine);
            Runtime.Colors.EntityRainbowifyHooks.Release(SourceEngine);
            UnloadBloomColors();
            UnloadFlashlightColors();
            UnloadStylegroundPositions();
            Runtime.Stylegrounds.InvisibleStylegroundHooks.Unload(SourceEngine);
            UnloadDecalContainers();
            UnloadActivators();
            fastCrystalDebrisPool?.Dispose();
            fastCrystalDebrisPool = null;
            Runtime.Feathers.FrostFeatherRuntime.Unload(SourceEngine);
            UnloadBoosterHooks();
            Runtime.Movement.ForcedFastfallHooks.Unload(SourceEngine);
            Runtime.Movement.NoMovementHooks.Unload(SourceEngine);
            Runtime.Bubbles.BubblerRuntime.Unload(SourceEngine);
            UnloadSprings();
            UnloadLightningColors();
            UnloadFlutterBirdSprites();
            UnloadDreamHooks();
            UnloadSpinnerHooks();
            Runtime.Lava.RisingLavaBlockerHooks.Unload(SourceEngine);
            UnloadLava();
            Runtime.Lighting.ColoredLampOutlines.Dispose(SourceEngine);
            Runtime.ZipMovers.CustomZipSpriteSource.Release(SourceEngine);
            Runtime.Expressions.FrostConditionRuntime.Release(SourceEngine);
        }

        public override void LoadContent(bool firstLoad)
        {
            Runtime.BadelineChasers.BadelineChaserFields.Prepare();
            Runtime.BadelineChasers.BadelineChaserBlockResources.For(SourceEngine).Load(SourceEngine);
            Runtime.Expressions.FrostConditionRuntime.For(SourceEngine).LoadContent();
            if (!firstLoad) return;
            var atlas = ((Celeste.Graphics.Content.ISourceGameGraphicsEnvironment)SourceEngine).GameGraphics.GameAtlas;
            fastCrystalDebrisPool ??= SourceEngine.EntityPools.RegisterScoped(() =>
                new Celeste.Mods.FrostHelper.Entities.Debris.FastCrystalDebris(atlas, SourceEngine));
        }
    }
}
