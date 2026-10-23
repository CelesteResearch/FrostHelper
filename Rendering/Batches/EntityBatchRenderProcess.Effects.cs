// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
using Celeste.Everest.Content.Registry;
using Celeste.Everest.Diagnostics.Logging;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Settings;
using Celeste.Graphics.Rendering.Effects;
using Celeste.Scene.Scenes;
using Celeste.Simulation.Gameplay.Environment.Core;

namespace Celeste.Mods.FrostHelper.Rendering.Batches
{
    internal sealed partial class EntityBatchRenderProcess
    {
        internal static void Invalidate(Celeste.Core.Scheduling.ISourceEngineContext engine, string id)
        {
            if (Owners.TryGetValue(engine, out var owner) && owner.effects.Remove(id, out var effect) &&
                !effect.IsDisposed)
                effect.Dispose();
        }

        private readonly Dictionary<string, SourceEffect> effects = new();

        private SourceEffect GetEffect(string id)
        {
            id = id.Replace('\\', '/');
            if (effects.TryGetValue(id, out var existing)) return existing;
            if (((IEverestContentRegistryEnvironment)engine).SourceContentRegistry.TryGet("Effects/" + id + ".cso",
                    out var asset))
            {
                try
                {
                    var effect = new SourceEffect(device.GraphicsDevice, asset.Data);
                    effects.Add(id, effect);
                    return effect;
                }
                catch (Exception ex)
                {
                    var logger = ((IEverestLoggerProcessEnvironment)engine).Logger;
                    logger.Error("FrostHelper", "Failed to load the shader " + id);
                    logger.Error("FrostHelper", "Exception: \n" + ex);
                }
            }

            Presentation.Notifications.FrostNotificationSink.NotifyMissingShader(engine, id);
            return graphics.FxTexture;
        }

        private static void ApplyParameters(SourceEffect effect, Dictionary<string, object> values, bool throwOnMissing)
        {
            foreach (var pair in values)
            {
                var parameter = effect.Parameters[pair.Key];
                if (parameter == null)
                {
                    if (throwOnMissing) throw new Exception("Shader doesn't have a " + pair.Key + " property!");
                    continue;
                }

                switch (parameter.ParameterType)
                {
                    case SourceEffectParameterType.Bool:
                        parameter.SetValue(Convert.ToBoolean(pair.Value));
                        break;
                    case SourceEffectParameterType.Single:
                        if (pair.Value is string text)
                            parameter.SetValue(Convert.ToSingle(text, CultureInfo.InvariantCulture));
                        else if (pair.Value is float number) parameter.SetValue(number);
                        break;
                    default:
                        throw new Exception(
                            $"Entity Batcher doesn't know how to set a parameter of type {parameter.ParameterType} for property {pair.Key}");
                }
            }
        }

        private void ApplyStandardParameters(SourceEffect effect)
        {
            var scene = engine.Frame.Scenes.CurrentScene;
            var level = scene as OriginalLevelSceneRoot;
            var parameters = effect.Parameters;
            parameters["DeltaTime"]?.SetValue(engine.DeltaTime);
            parameters["Time"]?.SetValue(((SourceSceneState)engine.Frame.Scenes.CurrentScene).TimeActive);
            parameters["Dimensions"]
                ?.SetValue(new Vector2(graphics.Buffers.Gameplay.Width, graphics.Buffers.Gameplay.Height));
            if (level != null)
            {
                parameters["CamPos"]?.SetValue(level.Camera.Position);
                parameters["ColdCoreMode"]?.SetValue(level.CoreMode == RoomCoreMode.Cold);
            }

            var viewport = device.GraphicsDevice.Viewport;
            parameters["TransformMatrix"]?.SetValue(Matrix4x4.Identity *
                                                    Matrix4x4.CreateOrthographicOffCenter(0f, viewport.Width,
                                                        viewport.Height, 0f, 0f, 1f));
            parameters["ViewMatrix"]?.SetValue(Matrix4x4.Identity);
            parameters["Photosensitive"]?.SetValue(((ISettingsContext)engine).Settings.DisableFlashes);
        }
    }
}
