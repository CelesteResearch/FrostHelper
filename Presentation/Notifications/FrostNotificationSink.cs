// SPDX-License-Identifier: MIT

using System.Runtime.CompilerServices;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.LevelLoader;
using Celeste.Game.Execution.Levels;
using Celeste.Everest.Content.Reloading;
using Celeste.Everest;
using Celeste.Mods.FrostHelper.Components.Lifecycle;
using Celeste.Scene.Scenes;

namespace Celeste.Mods.FrostHelper.Presentation.Notifications
{
    internal sealed class FrostNotificationSink(ISourceEngineContext engine)
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, FrostNotificationSink> Owners = new();

        internal static FrostNotificationSink For(ISourceEngineContext engine) =>
            Owners.GetValue(engine, static e => new(e));

        internal static void Release(ISourceEngineContext engine) => Owners.Remove(engine);

        internal static void NotifyMissingShader(ISourceEngineContext engine, string id) => For(engine).Push(
            new FrostNotification(EverestLogLevel.Error, "Shader not found: " + id)
            {
                Source = " | FrostHelper\\Code\\FrostHelper\\ModIntegration\\ShaderHelperIntegration.cs:165"
            });

        private FrostNotificationController justAddedController;

        internal void Push(FrostNotification notification)
        {
            var scene = NotificationScene();
            FrostNotificationController controller = null;
            if (scene.SourceScene.Entities.TryGetTrackedEntities(typeof(FrostNotificationController), out var tracked)
                && tracked.Count != 0) controller = tracked[0] as FrostNotificationController;
            if (controller == null && justAddedController == null)
            {
                justAddedController = new FrostNotificationController(engine);
                scene.AddEntity(justAddedController);
                justAddedController.Add(new OnAwakeCallbackComponent(() => justAddedController = null));
                controller = justAddedController;
            }

            (controller ?? justAddedController).Push(notification);
        }

        private SourceSceneState NotificationScene()
        {
            var scene = engine.Frame.Scenes.CurrentScene;
            if (scene is OriginalLevelSceneRoot level) return level;
            if (scene is OriginalLevelLoaderScene loader) return loader.Level ?? (SourceSceneState)scene;
            if (scene is EverestAssetReloadScene)
                return ((IEverestAssetReloadEnvironment)engine).AssetReload.ReturnToScene as OriginalLevelSceneRoot
                       ?? (SourceSceneState)scene;
            return (SourceSceneState)scene;
        }
    }
}
