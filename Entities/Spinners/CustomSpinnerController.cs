// SPDX-License-Identifier: MIT

using System;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Graphics.Rendering.Effects;
using Celeste.Mods.FrostHelper.Spinners.Rendering;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation;

namespace Celeste.Mods.FrostHelper.Entities.Spinners
{
    [CustomEntity("FrostHelper/CustomSpinnerController")]
    [SceneTracked]
    public sealed class CustomSpinnerController : SceneEntityState, ICustomSpinnerRenderSettings
    {
        private readonly ISourceEngineContext engine;
        public string LevelName { get; }
        public bool NoCycles { get; }
        public RoomPlayerSession Player { get; private set; }
        public PackedColor? FirstBorderColor { get; set; }
        public bool CanUseRenderTargetRender { get; set; }
        public bool CanUseBlackOutlineRenderTargetOpt { get; set; } = true;
        public float TimeUnpaused { get; private set; }
        public SourceEffect OutlineShader { get; }

        public CustomSpinnerController(ISourceEngineContext engine, string levelName, EntityData data = null,
            Func<string, SourceEffect> resolveOutlineShader = null)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration)
        {
            this.engine = engine;
            LevelName = levelName;
            if (data != null)
            {
                NoCycles = !data.Bool("cycles", true);
                OutlineShader = resolveOutlineShader?.Invoke(data.Attr("outlineShader"));
            }
        }

        public static CustomSpinnerController Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data)
            => new(level.EngineContext, level.Session.Level, data);

        public override void Awake(object scene)
        {
            base.Awake(scene);
            Player = ((ISourceScene)scene).SourceScene.Entities.GetTrackedEntity<RoomPlayerSession>(
                typeof(RoomPlayerSession).FullName);
        }

        public override void Update()
        {
            TimeUnpaused += engine.DeltaTime;
            if (Player?.Scene != Scene) Player = null;
        }
    }
}
