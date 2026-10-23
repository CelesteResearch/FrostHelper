using System;
using System.Collections.Generic;
using System.Reflection;
using Celeste.Core.Display;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Levels;
using Celeste.Scene.Tracking;
using Celeste.Simulation.Gameplay.Hazards.Lightning;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.FrostHelper.Runtime.Lighting
{
    public sealed class FrostLightningHooks : IDisposable
    {
        private readonly ISourceEngineContext engine;
        private readonly FrostLightningColors colors;
        private readonly List<IDisposable> hooks = new();

        public FrostLightningHooks(ISourceEngineContext engine, FrostLightningColors colors)
        {
            this.engine = engine;
            this.colors = colors;
            try
            {
                hooks.Add(new Hook(
                    typeof(LightningRendererState).GetMethod(nameof(LightningRendererState.Awake),
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly),
                    (Action<Action<LightningRendererState, object>, LightningRendererState, object>)Awake));
                hooks.Add(new Hook(
                    typeof(LightningRendererState).GetMethod(nameof(LightningRendererState.Reset),
                        BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly),
                    (Action<Action<LightningRendererState>, LightningRendererState>)Reset));
                hooks.Add(new Hook(
                    typeof(LightningRendererState).GetProperty("BufferClearColor",
                        BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly).GetMethod,
                    (Func<Func<LightningRendererState, PackedColor>, LightningRendererState, PackedColor>)
                    BufferClearColor));
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private bool Owns(object scene) =>
            scene is ISourceEngineContextScene context && ReferenceEquals(context.EngineContext, engine);

        private void Awake(Action<LightningRendererState, object> original, LightningRendererState renderer,
            object scene)
        {
            original(renderer, scene);
            if (!Owns(scene)) return;
            colors.GetColors(out var a, out var b);
            if (colors.Session.LightningColorA != null) FrostLightningColors.Change(renderer, new[] { a, b });
        }

        private void Reset(Action<LightningRendererState> original, LightningRendererState renderer)
        {
            if (renderer.Scene is OriginalLevelSceneRoot && Owns(renderer.Scene))
            {
                colors.GetColors(out var a, out var b);
                if (colors.Session.LightningColorA != null) FrostLightningColors.Change(renderer, new[] { a, b });
            }

            original(renderer);
        }

        private PackedColor BufferClearColor(Func<LightningRendererState, PackedColor> original,
            LightningRendererState renderer) =>
            Owns(renderer.Scene)
                ? PackedColor.Lerp(colors.ColorReader.Read(colors.FillColorString()) * colors.FillMultiplier(),
                    PackedColor.White, renderer.Fade)
                : original(renderer);

        public void Dispose()
        {
            for (int i = hooks.Count - 1; i >= 0; i--) hooks[i].Dispose();
            hooks.Clear();
        }
    }
}
