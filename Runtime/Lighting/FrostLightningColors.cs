using System;
using System.Reflection;
using System.Reflection.Emit;
using Celeste.Core.Display;
using Celeste.Core.Scheduling;
using Celeste.Everest.Reflection.Runtime;
using Celeste.Mods.FrostHelper.Color;
using Celeste.Mods.FrostHelper.Sessions;
using Celeste.Mods.FrostHelper.Sessions.Lighting;
using Celeste.Mods.FrostHelper.Triggers.Lighting;
using Celeste.Scene.Tracking;
using Celeste.Simulation.Gameplay.Hazards.Lightning;

namespace Celeste.Mods.FrostHelper.Runtime.Lighting
{
    public sealed class FrostLightningColors
    {
        private static readonly Action<LightningRendererState, PackedColor[]> SetElectricityColors =
            CreateElectricityColorsSetter();

        private readonly ISourceEngineContext engine;
        private readonly ColorReader colors;
        private readonly Func<FrostHelperSession> session;
        public ColorReader ColorReader => colors;
        public FrostHelperSession Session => session();

        public FrostLightningColors(ISourceEngineContext engine, ColorReader colors, Func<FrostHelperSession> session)
        {
            this.engine = engine;
            this.colors = colors;
            this.session = session;
        }

        public LightningColorTrigger FirstEntered(string groupId = "")
        {
            if (engine.Frame.Scenes.CurrentScene is not ISourceScene scene) return null;
            if (!scene.SourceScene.Entities.TryGetTrackedEntities(typeof(LightningColorTrigger), out var triggers))
                return null;
            foreach (LightningColorTrigger trigger in triggers)
                if (trigger.PlayerIsInside && trigger.GroupId == groupId)
                    return trigger;
            return null;
        }

        public string FillColorString(string previous = "f7b262") =>
            FirstEntered()?.FillColor ?? OrDefault(Session?.LightningFillColor, previous);

        public float FillMultiplier(float previous = .1f) =>
            FirstEntered()?.FillColorMultiplier ?? Session?.LightningFillColorMultiplier ?? previous;

        public void GetColors(out PackedColor a, out PackedColor b)
        {
            a = colors.Read(OrDefault(Session?.LightningColorA, "fcf579"));
            b = colors.Read(OrDefault(Session?.LightningColorB, "8cf7e2"));
        }

        public PackedColor FillColor(PackedColor previous)
        {
            if (FirstEntered() is { } trigger) return colors.Read(trigger.FillColor);
            if (Session?.LightningFillColor is { } value) return colors.Read(value);
            return previous;
        }

        public void Change(PackedColor[] palette, int? depth, string groupId, LightningGroupChange change,
            object actualScene)
        {
            var scene = (ISourceScene)actualScene;
            if (groupId == "")
            {
                var entities = scene.SourceScene.Entities;
                var renderer = entities.TryGetTrackedEntities(typeof(LightningRendererState), out var tracked) &&
                               tracked.Count > 0
                    ? tracked[0] as LightningRendererState
                    : null;
                Change(renderer, palette, depth);
            }

            var customType = ((ISourceRuntimeTypeDiscoveryEnvironment)engine).RuntimeTypeDiscovery
                .ResolveRuntimeTypeAlias("FrostHelper.CustomLightningRenderer");
            if (customType == null ||
                !scene.SourceScene.Entities.TryGetTrackedEntities(customType, out var customRenderers))
                return;
            foreach (var entity in customRenderers)
            {
                var renderer = (IFrostLightningGroupRenderer)entity;
                if (renderer.GroupId == groupId) renderer.Apply(change);
            }
        }

        public static void Change(LightningRendererState renderer, PackedColor[] palette, int? depth = null)
        {
            if (renderer == null) return;
            if (depth is { } value) renderer.Depth = value;
            SetElectricityColors(renderer, palette);
            var bolts = renderer.Bolts;
            for (int i = 0; i < bolts.Count; i++) bolts[i].Color = palette[i % 2];
        }

        private static Action<LightningRendererState, PackedColor[]> CreateElectricityColorsSetter()
        {
            var field = typeof(TiledLightningRendererState<LightningState>).GetField("electricityColors",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var setter = new DynamicMethod("FrostLightningElectricityColors", typeof(void),
                new[] { typeof(LightningRendererState), typeof(PackedColor[]) }, typeof(FrostLightningColors), true);
            var il = setter.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Stfld, field);
            il.Emit(OpCodes.Ret);
            return (Action<LightningRendererState, PackedColor[]>)setter.CreateDelegate(
                typeof(Action<LightningRendererState, PackedColor[]>));
        }

        public static string OrDefault(string value, string fallback) =>
            string.IsNullOrWhiteSpace(value) ? fallback : value;

        public static string Hex(PackedColor color) => color.A == 255
            ? $"{color.R:x2}{color.G:x2}{color.B:x2}"
            : $"{color.R:x2}{color.G:x2}{color.B:X2}{color.A:x2}";
    }
}
