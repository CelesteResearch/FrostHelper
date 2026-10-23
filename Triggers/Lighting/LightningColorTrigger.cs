using System.Linq;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Core.Display;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Modules.Runtime;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Mods.FrostHelper.Color;
using Celeste.Mods.FrostHelper.Runtime.Lighting;
using Celeste.Mods.FrostHelper.Sessions.Lighting;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Triggers.Foundation;

namespace Celeste.Mods.FrostHelper.Triggers.Lighting
{
    [CustomEntity("FrostHelper/LightningColorTrigger")]
    [SceneTracked]
    [RuntimeTypeAlias("FrostHelper.LightningColorTrigger")]
    public class LightningColorTrigger : SourceTriggerEntityState
    {
        private readonly FrostLightningColors runtime;
        private readonly PackedColor[] electricityColors;
        private readonly string groupId;
        private readonly int? newDepth;
        private readonly bool persistent;
        public string FillColor;
        public float FillColorMultiplier;
        public string GroupId => groupId;

        public LightningColorTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset) : this(engine, data,
            offset,
            ((ISourceEverestModulesEnvironment)engine).Modules.OfType<IFrostLightningEnvironment>().Single().Lightning)
        {
        }

        public LightningColorTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset,
            FrostLightningColors runtime) : base(
            ((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration,
            data.Position + offset, data.Width, data.Height, ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            this.runtime = runtime;
            persistent = data.Bool("persistent", false);
            var reader = runtime.ColorReader;
            electricityColors = new[]
                { reader.Read(data.Attr("color1", "fcf579")), reader.Read(data.Attr("color2", "8cf7e2")) };
            FillColor = FrostLightningColors.Hex(reader.Read(data.Attr("fillColor", "ffffff")));
            FillColorMultiplier = data.Float("fillColorMultiplier", .1f);
            groupId = data.Attr("targetGroup");
            if (!string.IsNullOrWhiteSpace(data.Attr("depth"))) newDepth = data.Int("depth");
        }

        public override void OnEnter(RoomPlayerSession player)
        {
            base.OnEnter(player);
            var change = new LightningGroupChange
            {
                Colors = electricityColors, FillColor = runtime.ColorReader.Read(FillColor),
                FillColorMultiplier = FillColorMultiplier, Depth = newDepth
            };
            runtime.Change(electricityColors, newDepth, groupId, change, Scene);
            if (persistent)
            {
                if (groupId == "")
                {
                    runtime.Session.LightningColorA = FrostLightningColors.Hex(electricityColors[0]);
                    runtime.Session.LightningColorB = FrostLightningColors.Hex(electricityColors[1]);
                    runtime.Session.LightningFillColor = FillColor;
                    runtime.Session.LightningFillColorMultiplier = FillColorMultiplier;
                }

                runtime.Session.ArbitraryLightningGroupChanges[groupId] = change;
            }
        }
    }
}
