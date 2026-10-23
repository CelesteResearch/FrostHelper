// SPDX-License-Identifier: MIT

using System;
using Celeste.Everest.Content.Assets;
using Celeste.Everest.Content.Registry;
using Celeste.Everest.Content.Reloading;
using Celeste.Everest.Diagnostics.Logging;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.LevelLoader;

namespace Celeste.Mods.FrostHelper.Rendering.Batches
{
    internal sealed class EntityBatchRenderLifecycle : IDisposable
    {
        private readonly Celeste.Core.Scheduling.ISourceEngineContext engine;
        private readonly EverestContentRegistry batcherContent;

        public EntityBatchRenderLifecycle(Celeste.Core.Scheduling.ISourceEngineContext engine)
        {
            this.engine = engine;
            batcherContent = ((IEverestContentRegistryEnvironment)engine).SourceContentRegistry;
            batcherContent.OnUpdate += ReloadBatcherShader;
        }

        public void Dispose()
        {
            batcherContent.OnUpdate -= ReloadBatcherShader;
            EntityBatchRenderProcess.Release(engine);
            Presentation.Notifications.FrostNotificationSink.Release(engine);
        }

        private void ReloadBatcherShader(EverestContentAsset from, EverestContentAsset to)
        {
            if (to.Format != "cso" && to.Format != ".cso") return;
            try
            {
                ((IEverestAssetReloadEnvironment)engine).AssetReload.Do("Reloading Shader", () =>
                {
                    string id = to.PathVirtual.Substring("Effects/".Length,
                        to.PathVirtual.Length - ".cso".Length - "Effects/".Length);
                    EntityBatchRenderProcess.Invalidate(engine, id);
                    ((IEverestLoggerProcessEnvironment)engine).Logger.Info("FrostHelper.ShaderHelper",
                        "Reloaded " + id);
                }, () => TryGetCurrentLevel()?.Reload());
            }
            catch (Exception ex)
            {
                ((IEverestLoggerProcessEnvironment)engine).Logger.Warn("FrostHelper.ShaderHelper", ex.ToString());
            }
        }

        private OriginalLevelSceneRoot TryGetCurrentLevel()
        {
            var scene = engine.Frame.Scenes.CurrentScene;
            if (scene is OriginalLevelSceneRoot level) return level;
            if (scene is OriginalLevelLoaderScene loader) return loader.Level;
            if (scene is EverestAssetReloadScene)
                return ((IEverestAssetReloadEnvironment)engine).AssetReload.ReturnToScene as OriginalLevelSceneRoot;
            return null;
        }
    }
}
