using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Celeste.Core.Scheduling;
using Celeste.Core.Components;
using Celeste.Core.Components.Timing;
using Celeste.Core.Display;
using Celeste.Core.Geometry;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Mathematics.Easing;
using Celeste.Core.Mathematics.LinearAlgebra;
using Celeste.Core.Rendering;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Modules.Runtime;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Controls.Source;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Entities.Platforms.ZipMovers.Motion;
using Celeste.Game.Presentation.Platforms.ZipMovers;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Rendering;
using Celeste.Graphics.Rendering.Drawing;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation.Audio.Sources;
using Celeste.Simulation.Gameplay.Environment.Core;
using Celeste.Simulation.Gameplay.Player.Intro;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Platforms.Collision;
using Celeste.Simulation.Presentation.PostProcessing.Bloom;
using Celeste.Simulation.Presentation.Lighting;
using Celeste.Simulation.Presentation.Particles.Definitions;
using Celeste.Mods.FrostHelper.Modules;
using Celeste.Mods.FrostHelper.Runtime.Rendering;
using Celeste.Mods.FrostHelper.Runtime.ZipMovers;

namespace Celeste.Mods.FrostHelper.Entities.Platforms.ZipMovers
{
    [CustomEntity("FrostHelper/CustomZipMover")]
    [SceneTracked(false)]
    [RuntimeTypeAlias("FrostHelper.CustomZipMover")]
    public sealed partial class CustomZipMover : SourceSolidEntityState
    {
        internal ISourceEngineContext Engine => SourceWorld.EngineContext;
        internal AtlasData GameAtlas => ((ISourceGameGraphicsEnvironment)Engine).GameGraphics.GameAtlas;
        private readonly FrostHelperModule module;
        internal FrostHelperModule Module => module;
        private Celeste.Mods.FrostHelper.Color.ColorReader Colors => Module.Colors;
        private PackedColor ReadColor(EntityData data, string key, string value) => Colors.Read(data.Attr(key, value));

        public static CustomZipMover Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data)
            => new(data, offset,
                ((ISourcePlatformCollisionScene)level).SolidWorldFactory.Create(data.Position + offset, false));

        public enum LineColor
        {
            Red,
            Blue,
            Black,
            Normal,
            Core,
            Custom
        }

        private bool isCore = false;
        private bool iceModeNext = false;
        private bool iceMode = false;

        private PackedColor ColdLineColor;
        private PackedColor ColdLightLineColor;
        private PackedColor HotLineColor;
        private PackedColor HotLightLineColor;

        internal bool drawLine;
        internal PackedColor tint = PackedColor.White;

        private readonly PackedColor _fillColor;
        internal readonly PackedColor _outlineColor;

        private Func<bool> CanActivate;

        private CustomZipSpriteSource HotSource { get; }
        private CustomZipSpriteSource ColdSource { get; }

        internal CustomZipSpriteSource CurrentSource { get; set; }

        private readonly string _sfxPath;

        private void OnChangeMode(RoomCoreMode coreMode)
        {
            iceModeNext = coreMode == RoomCoreMode.Cold;
        }

        private void CheckModeChange()
        {
            if (iceModeNext != iceMode)
            {
                iceMode = iceModeNext;
                ToggleSprite();
            }
        }

        private void ToggleSprite()
        {
            if (iceMode)
            {
                ropeColor = ColdLineColor;
                ropeLightColor = ColdLightLineColor;
                CurrentSource = ColdSource ?? HotSource;
                SpeedMult = SpeedMultIce;
            }
            else
            {
                ropeColor = HotLineColor;
                ropeLightColor = HotLightLineColor;
                CurrentSource = HotSource;
                SpeedMult = SpeedMultNormal;
            }

            CreateSprites();
        }

        public bool Rainbow;

        public CustomZipMover(EntityData data, Vector2 offset, ISourceSolidWorld world) : base(data.Position + offset,
            data.Width, data.Height, false, world)
        {
            module = ((ISourceEverestModulesEnvironment)Engine).Modules.OfType<FrostHelperModule>().Single();
            tint = Colors.Read(data.Attr("tint", "ffffff"));
            Rainbow = data.Bool("rainbow", false);
            drawLine = data.Bool("showLine", true);

            string hexcolor, hexlightcolor;
            switch (data.Enum("color", LineColor.Custom)) // legacy support
            {
                case LineColor.Red:
                    hexcolor = "e62e00"; // 230 46 0
                    hexlightcolor = "ff5c33";
                    HotSource = CustomZipSpriteSource.Get(Engine, "objects/FrostHelper/customZipMover/redcog");
                    break;
                case LineColor.Blue:
                    hexcolor = "006bb3"; // 0 107 179
                    hexlightcolor = "0099ff";
                    HotSource = CustomZipSpriteSource.Get(Engine, "objects/FrostHelper/customZipMover/redcog/cold");
                    break;
                case LineColor.Black:
                    hexcolor = "000000";
                    hexlightcolor = "1a1a1a";
                    HotSource = CustomZipSpriteSource.Get(Engine, "objects/FrostHelper/customZipMover/blackcog");
                    break;
                case LineColor.Normal:
                    hexcolor = "663931";
                    hexlightcolor = "9b6157";
                    HotSource = CustomZipSpriteSource.Get(Engine, "objects/zipmover");
                    break;
                case LineColor.Core:
                    hexcolor = "e62e00"; // 230 46 0
                    hexlightcolor = "ff5c33";
                    ColdLineColor = Colors.Read("006bb3");
                    ColdLightLineColor = Colors.Read("0099ff");
                    HotLineColor = Colors.Read(hexcolor);
                    HotLightLineColor = Colors.Read(hexlightcolor);
                    isCore = true;
                    HotSource = CustomZipSpriteSource.Get(Engine, "objects/FrostHelper/customZipMover/redcog");
                    ColdSource = CustomZipSpriteSource.Get(Engine, "objects/FrostHelper/customZipMover/redcog/cold");
                    break;
                case LineColor.Custom:
                    hexcolor = data.Attr("lineColor", "663931");
                    hexlightcolor = data.Attr("lineLightColor", "ff5c33");
                    ColdLineColor = ReadColor(data, "coldLineColor", "663931");
                    ColdLightLineColor = ReadColor(data, "coldLineLightColor", "663931");
                    HotLineColor = ReadColor(data, "lineColor", "663931");
                    HotLightLineColor = ReadColor(data, "lineLightColor", "ff5c33");
                    var directory = data.Attr("directory", "objects/zipmover");
                    // legacy support - bluecog was moved to redcog/cold to make core mode work correctly with them without hardcoding. We need this to make maps using the old directory still work. Ahorn doesn't have this however, so that new users won't accidentaly use this.
                    if (directory == "objects/FrostHelper/customZipMover/bluecog")
                        directory = "objects/FrostHelper/customZipMover/redcog/cold";

                    HotSource = CustomZipSpriteSource.Get(Engine, directory);
                    isCore = data.Bool("isCore", false);
                    if (isCore)
                    {
                        ColdSource = CustomZipSpriteSource.Get(Engine, directory + "/cold");
                    }

                    break;
                default:
                    throw new ArgumentOutOfRangeException("color", data.Enum("color", LineColor.Normal), null);
            }

            CurrentSource = HotSource;

            if (isCore)
            {
                OwnedComponents.Add(new CoreModeListenerState(OnChangeMode));
            }

            ropeColor = Colors.Read(hexcolor);
            ropeLightColor = Colors.Read(hexlightcolor);
            temp = new AtlasEntry();
            sfx = new SoundSourceComponentState(Engine);
            Depth = -9999;
            start = Position;
            target = data.Nodes[0] + offset;
            OwnedComponents.Add(new SimulationCoroutine(Engine, Sequence()));
            OwnedComponents.Add(new LightOccludeState(1f));

            CreateSprites();

            float bloomAlpha = data.Float("bloomAlpha", 1f);
            if (bloomAlpha != 0.0f)
            {
                OwnedComponents.Add(bloom = new BloomPointState(bloomAlpha, data.Float("bloomRadius", 6f)));
                bloom.Position = new Vector2(Width / 2f, 4f);
            }

            SurfaceSoundIndex = 7;
            sfx.Position = new Vector2(Width, Height) / 2f;
            OwnedComponents.Add(sfx);
            _sfxPath = data.Attr("sfx", "event:/game/01_forsaken_city/zip_mover");

            if (data.Has("percentage"))
            {
                SpeedMultNormal = data.Float("percentage", 100f) / 100f;
                SpeedMultIce = SpeedMultNormal / 4;
            }
            else
            {
                SpeedMultNormal = data.Float("speedMultiplier", 1f);
                SpeedMultIce = data.Float("coldSpeedMultiplier", 0.25f);
            }

            SpeedMult = SpeedMultNormal;

            var legacyFillMiddle = data.Bool("fillMiddle", true);
            _fillColor = ReadColor(data, "fillColor", legacyFillMiddle ? "000000" : "00000000");
            _outlineColor = ReadColor(data, "outlineColor", legacyFillMiddle ? "000000" : "00000000");
            CanActivate = FrostZipActivation.Create(data.Attr("canActivateFlag"));
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            if (isCore)
            {
                iceModeNext = iceMode = ((OriginalLevelSceneRoot)Scene).CoreMode == RoomCoreMode.Cold;
                ToggleSprite();
            }

            CustomZipPathRenderer.Ensure(Engine, scene);
        }

        public override void Update()
        {
            base.Update();
            CheckModeChange();
            if (bloom != null)
                bloom.Y = streetlight.CurrentAnimationFrame * 3;
        }

        public void CreateSprites()
        {
            OwnedComponents.RemoveAll<Celeste.Graphics.Animation.SpriteImage>();
            OwnedComponents.RemoveAll<Sprite>();

            OwnedComponents.Add(streetlight = new Sprite(Engine, GameAtlas, CurrentSource.LightPath));
            streetlight.Add("frames", "", 1f);
            streetlight.Play("frames", false, false);
            streetlight.Active = false;
            streetlight.SetAnimationFrame(1);
            streetlight.Position = new Vector2(Width / 2f - streetlight.Width / 2f, 0f);
        }
    }
}
