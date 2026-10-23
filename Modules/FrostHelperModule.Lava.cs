// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.FrostHelper.Components.Flags;
using Celeste.Mods.FrostHelper.Components.Lava;
using Celeste.Mods.FrostHelper.Runtime.Lava;
using Celeste.Simulation.Runtime.Session;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.FrostHelper.Modules
{
    public sealed partial class FrostHelperModule
    {
        private Hook flagHook;
        private LavaRenderTargetPool lavaBuffers;
        private readonly Dictionary<string, List<CustomLavaRect.WaveData>> lavaWaves = new(StringComparer.Ordinal);
        internal LavaRenderTargetPool LavaBuffers => lavaBuffers ??= new(SourceEngine);

        internal List<CustomLavaRect.WaveData> GetLavaWaves(string specification)
        {
            if (lavaWaves.TryGetValue(specification, out var waves)) return waves;
            lavaWaves.Add(specification, waves = CustomLavaRect.WaveData.ParseWaves(specification));
            return waves;
        }

        private delegate void SetFlagOriginal(ChapterCompletionSession session, string flag, bool value);

        private delegate void SetFlagHook(SetFlagOriginal original, ChapterCompletionSession session, string flag,
            bool value);

        internal void EnsureFlagHook()
        {
            flagHook ??= new Hook(typeof(ChapterCompletionSession).GetMethod(nameof(ChapterCompletionSession.SetFlag)),
                (SetFlagHook)OnSetFlag);
        }

        private void OnSetFlag(SetFlagOriginal original, ChapterCompletionSession session, string flag, bool value)
        {
            if (SourceEngine.Frame.Scenes.CurrentScene is not OriginalLevelSceneRoot level ||
                !ReferenceEquals(level.EngineContext, SourceEngine) || !ReferenceEquals(level.Session, session))
            {
                original(session, flag, value);
                return;
            }

            var definition = level.Scheduler.Catalog.Find(typeof(FlagListener));
            if (definition == null || !level.Scheduler.Components.Components.TryGetValue(definition.Name,
                                       out var listeners)
                                   || listeners.Count == 0)
            {
                original(session, flag, value);
                return;
            }

            bool before = session.GetFlag(flag);
            original(session, flag, value);
            foreach (FlagListener listener in listeners)
                if ((listener.Flag == null || listener.Flag == flag) && (!listener.MustChange || before != value))
                    listener.OnSet(session, flag, value);
        }

        private void UnloadLava()
        {
            flagHook?.Dispose();
            flagHook = null;
            lavaBuffers?.Dispose();
            lavaBuffers = null;
            lavaWaves.Clear();
        }
    }
}
