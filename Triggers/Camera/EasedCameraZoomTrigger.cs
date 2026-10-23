using Celeste.Game.Execution.Levels;
using Celeste.Core.Components;
using System;
using System.Numerics;
using Celeste.Core.Mathematics.Easing;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Settings;
using Celeste.Mods.FrostHelper.Animation.Easing;
using Celeste.Mods.FrostHelper.Entities.Controllers.Camera;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Triggers.Foundation;

namespace Celeste.Mods.FrostHelper.Triggers.Camera
{
    [CustomEntity("CCH/EasedCameraZoomTrigger", "FrostHelper/EasedCameraZoomTrigger")]
    [RuntimeTypeAlias("FrostHelper.EasedCameraZoomTrigger")]
    public class EasedCameraZoomTrigger : SourceTriggerEntityState
    {
        private readonly ISourceEngineContext engine;
        public Func<float, float> Easer;
        public float TargetZoom;
        public bool RevertOnLeave;
        public ZoomTriggerRevertMode RevertMode;
        public float EaseDuration;
        public bool FocusOnPlayer;
        public Vector2? FocusPoint;
        public bool DisableInPhotosensitiveMode;
        private Vector2? prevFocusPoint;
        private float initialZoom;
        private bool prevFocusOnPlayer;

        public EasedCameraZoomTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration,
                data.Position + offset, data.Width, data.Height, ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            this.engine = engine;
            Easer = new FrostEasingResolver(((ISourceEasingEnvironment)engine).EasingArithmetic).Resolve(
                data.Attr("easing", "Linear"));
            TargetZoom = data.Float("targetZoom", 2f);
            RevertOnLeave = data.Bool("revertOnLeave", false);
            EaseDuration = data.Float("easeDuration", 1f);
            FocusOnPlayer = data.Bool("focusOnPlayer", true);
            RevertMode = data.Enum("revertMode", ZoomTriggerRevertMode.RevertToNoZoom);
            DisableInPhotosensitiveMode = data.Bool("disableInPhotosensitiveMode", true);
            FocusPoint = data.FirstNodeNullable(offset);
        }

        public OriginalLevelSceneRoot Level => Scene as OriginalLevelSceneRoot;
        public ZoomManager ZoomManager => ZoomManager.Ensure(engine, Scene);
        private Vector2? GetFocusPoint() => FocusOnPlayer ? null : FocusPoint;
        private bool GetOnlyY() => !FocusOnPlayer;

        public override void OnEnter(RoomPlayerSession player)
        {
            var manager = ZoomManager;
            initialZoom = RevertMode switch
            {
                ZoomTriggerRevertMode.RevertToNoZoom => 1f,
                ZoomTriggerRevertMode.RevertToPreviousZoom => manager.TargetZoom,
                _ => throw new NotImplementedException()
            };
            prevFocusPoint = manager.FocusPoint;
            prevFocusOnPlayer = manager.FocusZoomOnPlayer;
            manager.FocusZoomOnPlayer = FocusOnPlayer;
            if (!(DisableInPhotosensitiveMode && ((ISettingsContext)engine).Settings.DisableFlashes))
                manager.DoZoom(Easer, TargetZoom, EaseDuration, GetFocusPoint(), GetOnlyY());
        }

        public override void OnStay(RoomPlayerSession player) => ZoomManager.FocusZoomOnPlayer = FocusOnPlayer;

        public override void OnLeave(RoomPlayerSession player)
        {
            if (RevertOnLeave)
            {
                ZoomManager.DoZoom(Easer, initialZoom, EaseDuration, prevFocusPoint, GetOnlyY());
                ZoomManager.FocusZoomOnPlayer = prevFocusOnPlayer;
            }
        }
    }
}
