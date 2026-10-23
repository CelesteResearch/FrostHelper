using Celeste.Mods.FrostHelper.Components.Lifecycle;
using System;
using System.Collections;
using System.Numerics;
using System.Runtime.CompilerServices;
using Celeste.Core.Components;
using Celeste.Core.Mathematics;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Levels;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation;

namespace Celeste.Mods.FrostHelper.Entities.Controllers.Camera
{
    [SceneTracked(false)]
    [RuntimeTypeAlias("FrostHelper.ZoomManager")]
    public class ZoomManager : SceneEntityState
    {
        private sealed class Pending
        {
            internal ZoomManager Value;
        }

        private static readonly ConditionalWeakTable<object, Pending> PendingControllers = new();
        private readonly ISourceEngineContext engine;
        private SimulationCoroutine zoomCoroutine;
        public float TargetZoom = 1f;
        public bool FocusZoomOnPlayer;
        public Vector2? FocusPoint;
        public bool OnlyY;
        public static Vector2 DefaultZoomFocusPoint = new Vector2(320f, 180f) / 2f;
        public OriginalLevelSceneRoot Level => Scene as OriginalLevelSceneRoot;

        public ZoomManager(ISourceEngineContext engine)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, Vector2.Zero)
        {
            this.engine = engine;
            Depth = -1000000;
        }

        public static ZoomManager Ensure(ISourceEngineContext engine, object scene)
        {
            var scheduler = ((ISourceScene)scene).SourceScene;
            var tracked =
                scheduler.Entities.GetTrackedEntity<ZoomManager>(
                    scheduler.Catalog.Find(typeof(ZoomManager)).RegistryKey);
            if (tracked != null) return tracked;
            var pending = PendingControllers.GetValue(scene, _ => new Pending());
            if (pending.Value == null)
            {
                pending.Value = new ZoomManager(engine);
                pending.Value.OwnedComponents.Add(new OnAwakeCallbackComponent(() => pending.Value = null));
                scheduler.QueueSourceEntity(pending.Value);
            }

            return pending.Value;
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            Level.ResetZoom();
        }

        public override void Update()
        {
            if (Level.Zoom == TargetZoom) SetZoomFocusPoint(GetTargetZoomFocusPoint());
            base.Update();
        }

        public void DoZoom(Func<float, float> easer, float zoom, float duration, Vector2? focusPoint, bool onlyY)
        {
            OnlyY = onlyY;
            if (zoom != TargetZoom)
            {
                StopZooming();
                OwnedComponents.Add(zoomCoroutine =
                    new SimulationCoroutine(engine, ZoomRoutine(easer, zoom, duration, focusPoint)));
            }
        }

        public void StopZooming()
        {
            if (zoomCoroutine != null) OwnedComponents.Remove(zoomCoroutine);
        }

        public IEnumerator ZoomRoutine(Func<float, float> easer, float zoom, float duration, Vector2? focusPoint)
        {
            FocusPoint = focusPoint;
            TargetZoom = zoom;
            float from = Level.Zoom;
            Vector2 prevFocus = Level.ZoomFocusPoint;
            for (float p = 0f; p < 1f; p += engine.DeltaTime / duration)
            {
                float amount = easer(ScalarMath.Clamp(p, 0f, 1f));
                Level.Zoom = Level.ZoomTarget = ScalarMath.Lerp(from, zoom, amount,
                    ((ISourceScalarEnvironment)engine).ScalarArithmetic);
                SetZoomFocusPoint(Vector2Interpolation.Lerp(prevFocus, GetTargetZoomFocusPoint(), amount,
                    ((ISourceScalarEnvironment)engine).ScalarArithmetic));
                yield return null;
            }

            Level.Zoom = Level.ZoomTarget = zoom;
            SetZoomFocusPoint(GetTargetZoomFocusPoint());
        }

        private void SetZoomFocusPoint(Vector2 position)
        {
            if (OnlyY) Level.ZoomFocusPoint.Y = position.Y;
            else Level.ZoomFocusPoint = position;
        }

        public Vector2 GetTargetZoomFocusPoint()
        {
            if (TargetZoom == 1f) return DefaultZoomFocusPoint;
            if (FocusPoint.HasValue) return FocusPoint.Value - Level.Camera.Position;
            if (FocusZoomOnPlayer)
            {
                var scheduler = ((ISourceScene)Scene).SourceScene;
                var player =
                    scheduler.Entities.GetTrackedEntity<RoomPlayerSession>();
                if (player != null) return player.Position - Level.Camera.Position;
            }

            return DefaultZoomFocusPoint;
        }
    }
}
