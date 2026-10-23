// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Reflection;
using Celeste.Core.Geometry;
using Celeste.Everest.Gameplay.Triggers.Hazards;
using Celeste.Game.Controls.Source;
using Celeste.Game.Execution.Engine.Actions;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Mods.FrostHelper.Entities.Spinners;
using Celeste.Physics.Collision;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.FrostHelper.Modules
{
    public sealed partial class FrostHelperModule
    {
        private readonly List<IDisposable> spinnerHooks = new();
        private bool spinnerHooksLoaded;

        private static readonly FieldInfo ShatterMode =
            typeof(CrystalShatterTrigger).GetField("mode", BindingFlags.NonPublic | BindingFlags.Instance);

        internal void EnsureSpinnerHooks()
        {
            if (spinnerHooksLoaded) return;
            spinnerHooksLoaded = true;
            spinnerHooks.Add(new Hook(typeof(CrystalShatterTrigger).GetMethod(nameof(CrystalShatterTrigger.OnEnter)),
                (Action<Action<CrystalShatterTrigger, RoomPlayerSession>, CrystalShatterTrigger, RoomPlayerSession>)
                ShatterTriggerEnter));
            spinnerHooks.Add(new Hook(
                typeof(RoomPlayerSession).GetMethod("UpdateSummitLaunch",
                    BindingFlags.NonPublic | BindingFlags.Instance),
                (Func<Func<RoomPlayerSession, int>, RoomPlayerSession, int>)SummitLaunchUpdate));
        }

        private void UnloadSpinnerHooks()
        {
            for (int i = spinnerHooks.Count - 1; i >= 0; i--) spinnerHooks[i].Dispose();
            spinnerHooks.Clear();
            spinnerHooksLoaded = false;
        }

        private void ShatterTriggerEnter(Action<CrystalShatterTrigger, RoomPlayerSession> original,
            CrystalShatterTrigger trigger, RoomPlayerSession player)
        {
            if (trigger.Scene is OriginalLevelSceneRoot level && ReferenceEquals(level.EngineContext, SourceEngine))
            {
                var entities = ((ISourceScene)level).SourceScene.Entities.GetTrackedEntities(typeof(CustomSpinner));
                if (entities.Count > 0)
                {
                    bool all = (CrystalShatterTrigger.Modes)ShatterMode.GetValue(trigger) ==
                               CrystalShatterTrigger.Modes.All;
                    if (all)
                        ((IOriginalGameplayProcess)SourceEngine).AudioSession.Play(
                            "event:/game/06_reflection/boss_spikes_burst");
                    foreach (object entity in entities)
                    {
                        var spinner = (CustomSpinner)entity;
                        if (all || SourceEntityCollision.Check(trigger, spinner)) spinner.Destroy();
                    }
                }
            }

            original(trigger, player);
        }

        private int SummitLaunchUpdate(Func<RoomPlayerSession, int> original, RoomPlayerSession player)
        {
            int state = original(player);
            if (player.Scene is not OriginalLevelSceneRoot level ||
                !ReferenceEquals(level.EngineContext, SourceEngine)) return state;
            var rectangle = new IntRect((int)(player.Position.X - 4f), (int)(player.Position.Y - 40f), 8, 12);
            foreach (object entity in ((ISourceScene)level).SourceScene.Entities.GetTrackedEntities(
                         typeof(CustomSpinner)))
            {
                var spinner = (CustomSpinner)entity;
                if (!spinner.Collidable || !SourceEntityGeometryCollision.Rectangle(spinner, rectangle)) continue;
                spinner.Destroy();
                level.Shake(.3f);
                ((IOriginalGameInputProcessEnvironment)SourceEngine).InputSource.Rumble(RumbleStrength.Medium,
                    RumbleLength.Short);
                OriginalCelesteFreeze.Freeze(SourceEngine, .01f);
                break;
            }

            return state;
        }
    }
}
