// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Randomness;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Modules.Runtime;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Entities.Hazards.Spinners.Crystal;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.FrostHelper.Collider.Spinners;
using Celeste.Mods.FrostHelper.Components.Platforms;
using Celeste.Mods.FrostHelper.Content.Spinners;
using Celeste.Mods.FrostHelper.Modules;
using Celeste.Mods.FrostHelper.Runtime.Spinners;
using Celeste.Mods.FrostHelper.Spinners.Rendering;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Collision;
using Celeste.Simulation.Gameplay.Hazards.Spinners.Crystal;
using Celeste.Simulation.Gameplay.Holdables;
using Celeste.Simulation.Gameplay.Collision.Blockers;
using Celeste.Simulation.Gameplay.Player.Collision.Components;
using Celeste.Simulation.Gameplay.Platforms.StaticMovers;
using Celeste.Simulation.Presentation.PostProcessing.Bloom;

namespace Celeste.Mods.FrostHelper.Entities.Spinners
{
    [CustomEntity("FrostHelper/IceSpinner", "FrostHelperExt/CustomBloomSpinner")]
    [SceneTracked(true)]
    [global::Celeste.Core.Reflection.Runtime.RuntimeTypeAlias("FrostHelper.CustomSpinner")]
    public partial class CustomSpinner : OriginalCrystalStaticSpinner, ICustomSpinnerRenderSource
    {
        private readonly FrostSpinnerServices services;
        private readonly List<SpinnerRenderImage> sprites = new();
        private readonly List<SpinnerRenderImage> fills = new();
        private readonly List<(SpinnerRenderImage Image, float Scale)> decorations = new();
        private Vector2 decorationPosition;
        private bool decorationCreated;
        private readonly BloomPointState bloom;
        private readonly string destroyColor;
        private readonly bool isCore;
        private readonly string hitboxText;
        private readonly float scale;
        private int lastDepth;
        private bool forcedInvisible;
        private SpinnerRenderGroup group;

        public CustomSpinnerController Controller { get; private set; }
        public SpinnerSpriteSource SpriteSource { get; private set; }
        public IReadOnlyList<SpinnerRenderImage> Sprites => sprites;
        public IReadOnlyList<SpinnerRenderImage> ConnectionFills => fills;
        public Vector2 ShakeAmt { get; private set; }
        public float ImageScale { get; }
        public float TimeUnpaused => Controller.TimeUnpaused;
        public PackedColor Tint { get; private set; }
        public PackedColor BorderColor { get; private set; }
        public bool RenderBorder { get; private set; }
        public bool RegisteredToRenderers { get; set; }
        public bool HasCollider { get; private set; }
        public bool ShouldBeCollidable { get; private set; } = true;
        public bool ColliderDisabledExternally { get; set; }
        public bool Rainbow { get; private set; }
        public bool SingleFGImage { get; }
        public bool MoveWithWind { get; }
        public int AttachGroup { get; }
        public int DestroyDebrisCount { get; }
        public SpinnerCollisionMode DashThrough { get; }
        public SpinnerCollisionMode PlayerCollisionMode { get; }
        public SpinnerCollisionMode HoldableCollisionMode { get; }

        public static CustomSpinner Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) => new(data, offset, level.EngineContext);

        public CustomSpinner(EntityData data, Vector2 positionOffset, ISourceEngineContext engine)
            : base(data.Position + positionOffset, data.Bool("attachToSolid"), CrystalSpinnerColor.Blue,
                engine, GetSourceOffset(engine))
        {
            var module = ((ISourceEverestModulesEnvironment)engine).Modules.OfType<FrostHelperModule>().Single();
            module.EnsureSpinnerHooks();
            services = module.Spinners;
            ID = data.ID;
            scale = data.Float("scale", 1f);
            ImageScale = data.Float("imageScale", 1f);
            Rainbow = data.Bool("rainbow");
            Tint = module.Colors.Read(data.Attr("tint", "ffffff"));
            RenderBorder = data.Bool("drawOutline", true);
            BorderColor = RenderBorder
                ? module.Colors.Read(data.Attr("borderColor", "000000"))
                : PackedColor.Transparent;
            RenderBorder = BorderColor != PackedColor.Transparent;
            DestroyDebrisCount = data.Int("debrisCount", 8);
            DashThrough = data.Values != null && data.Values.TryGetValue("dashThrough", out var dash)
                ? dash switch
                {
                    true => SpinnerCollisionMode.PassThrough,
                    string text when Enum.TryParse(text, true, out SpinnerCollisionMode mode) => mode,
                    _ => SpinnerCollisionMode.Kill
                }
                : SpinnerCollisionMode.Kill;
            PlayerCollisionMode = data.Enum("onPlayer", SpinnerCollisionMode.Kill);
            HoldableCollisionMode = data.Enum("onHoldable", SpinnerCollisionMode.PassThrough);
            SingleFGImage = data.Bool("singleFGImage");
            MoveWithWind = data.Bool("moveWithWind");
            destroyColor = data.Attr("destroyColor", "639bff");
            isCore = data.Bool("isCore");
            hitboxText = data.Attr("hitbox", "C,6,0,0;R,16,4,-8,-3");
            HasCollider = data.Bool("collidable", true) && scale > 0f && !string.IsNullOrWhiteSpace(hitboxText);
            // Original disabled spinners retain no collider; callbacks are inert while Collidable is false.
            Collider = HasCollider
                ? SpinnerHitboxReader.Create(hitboxText, scale, VectorArithmetic, module.Warn)
                : null;
            if (!HasCollider)
            {
                ShouldBeCollidable = Collidable = false;
                Remove(Get<PlayerColliderComponentState>());
                Remove(Get<HoldableColliderState>());
                Remove(Get<LedgeBlockerState>());
            }

            ColliderDisabledExternally = !HasCollider;
            Depth = lastDepth = data.Int("depth", -8500);
            AttachGroup = data.Int("attachGroup", -1);
            if (AttachToSolid)
            {
                var mover = Get<SourceStaticMoverState>();
                if (AttachGroup != -1)
                {
                    var solidChecker = mover.SolidChecker;
                    Remove(mover);
                    Add(mover = new GroupedStaticMover(AttachGroup, true));
                    mover.SolidChecker = solidChecker;
                }

                mover.OnShake = OnShake;
                mover.OnDisable = () =>
                {
                    if (Visible) Unregister();
                    Active = Visible = Collidable = ShouldBeCollidable = false;
                };
                mover.OnEnable = () =>
                {
                    Active = ShouldBeCollidable = true;
                    if (!ColliderDisabledExternally) Collidable = true;
                };
                mover.OnDestroy = RemoveSelf;
            }

            float alpha = data.Float("bloomAlpha", 0f);
            if (alpha != 0f)
                Add(bloom = new BloomPointState(Collidable ? Collider.Center : Position + new Vector2(8f), alpha,
                    data.Float("bloomRadius")));
            SetVisible(false);
            SpriteSource = services.GetSprites(data.Attr("directory", "danger/FrostHelper/icecrystal"),
                data.Attr("spritePathSuffix"));
        }

        private static float GetSourceOffset(ISourceEngineContext engine)
        {
            var random = ((ISourceRandomEnvironment)engine).Random;
            random.Next(); // Old Frost atlas choice remains part of deterministic spinner cycles.
            return SourceRandomMath.NextFloat(random);
        }

        protected override bool EnableCoreModeListener => false;
        protected override bool IsRainbow => Rainbow;

        public override void Added(object scene)
        {
            base.Added(scene);
            group = services.ForScene((OriginalLevelSceneRoot)scene).Get(Depth);
            if (SpriteSource.HasDeco) group.EnsureDecorationRenderer();
        }

        public override void Awake(object scene)
        {
            Controller = services.ForScene((OriginalLevelSceneRoot)scene).GetController();
            UpdateController();
            base.Awake(scene);
        }

        private void UpdateController()
        {
            if (BorderColor != PackedColor.Black) Controller.CanUseBlackOutlineRenderTargetOpt = false;
            if (Controller.FirstBorderColor == null)
            {
                Controller.FirstBorderColor = BorderColor;
                Controller.CanUseRenderTargetRender = true;
            }
            else if (Controller.FirstBorderColor != BorderColor) Controller.CanUseRenderTargetRender = false;
        }

        private void SetVisible(bool value)
        {
            Visible = value;
            if (bloom != null) bloom.Visible = value;
        }

        public void SetForcedInvisible(bool value)
        {
            if (forcedInvisible == value) return;
            forcedInvisible = value;
            if (value)
            {
                SetVisible(false);
                Unregister();
            }
            else if (InView())
            {
                group.Register(this);
                SetVisible(true);
                if (!expanded) CreateSprites();
                if (Rainbow) UpdateHue();
            }
        }

        public override void Update()
        {
            if (!Visible && !forcedInvisible)
            {
                ShouldBeCollidable = Collidable = false;
                if (InView())
                {
                    group.Register(this);
                    SetVisible(true);
                    if (!expanded) CreateSprites();
                    if (Rainbow) UpdateHue();
                }
            }
            else
            {
                if (lastDepth != Depth)
                    SetDepth(Depth);
                var clock = ((ISourceScene)Scene).SourceScene.Clock;
                if (!forcedInvisible)
                {
                    if (Rainbow && (Controller.NoCycles || clock.OnInterval(.08f, offset, engine))) UpdateHue();
                    if (clock.OnInterval(.25f, offset, engine) && !InView())
                    {
                        SetVisible(false);
                        Unregister();
                    }
                }

                if (HasCollider && (Controller.NoCycles || clock.OnInterval(.05f, offset, engine)))
                {
                    var player =
                        ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntity<RoomPlayerSession>(
                            typeof(RoomPlayerSession).FullName);
                    if (player != null)
                    {
                        ShouldBeCollidable = Math.Abs(player.Position.X - Position.X) < 128f &&
                                             Math.Abs(player.Position.Y - Position.Y) < 128f;
                        Collidable = !ColliderDisabledExternally && ShouldBeCollidable;
                    }
                }
            }

            if (MoveWithWind)
            {
                int move = (int)(Math.Clamp(Math.Abs(Level.Wind.X) / 800f, 0f, 1f) * 5f);
                if (Level.Wind.X < 0f) move = -move;
                Position = new Vector2(Position.X + move, Position.Y);
                if (HasCollider) Collider.Position = new Vector2(Collider.Position.X + move, Collider.Position.Y);
            }
        }

        protected override bool InView()
        {
            var camera = Level.Camera;
            var distance = SpriteSource.CullingDistance * ImageScale;
            var diff = Position - camera.Position;
            diff += distance;
            if (diff.X <= 0f | diff.Y <= 0f) return false;
            diff -= new Vector2(camera.Viewport.Width, camera.Viewport.Height);
            diff -= distance;
            diff -= distance;
            return diff.X < 0f & diff.Y < 0f;
        }

        public override PackedColor GetHue(Vector2 position)
        {
            return services.GetHue(Scene, position);
        }

        public override void UpdateHue()
        {
            services.InitializeHue();
            foreach (var image in sprites) image.Color = GetHue(Position + image.Position);
            foreach (var image in fills) image.Color = GetHue(Position + image.Position);
        }

        public void SetColor(PackedColor value, bool force = false)
        {
            if (!force && Tint == value) return;
            Tint = value;
            foreach (var image in sprites) image.Color = value;
            foreach (var image in fills) image.Color = value;
        }

        public void SetBorderColor(PackedColor value)
        {
            if (BorderColor == value) return;
            BorderColor = value;
            UpdateController();
        }

        private void OnShake(Vector2 value)
        {
            foreach (var image in sprites) image.Position += value;
            ShakeAmt += value;
        }

        private void Unregister() => group?.Unregister(this);

        public void SetDepth(int value)
        {
            if (lastDepth == value) return;
            Unregister();
            Depth = lastDepth = value;
            group = services.ForScene(Level).Get(value);
            if (SpriteSource.HasDeco) group.EnsureDecorationRenderer();
            group.Register(this);
            Level.Scheduler.OnEndOfFrame += () => Level.Scheduler.CommitQueuedEntityLists();
        }

        public new void ClearSprites()
        {
            if (!expanded) return;
            expanded = false;
            SetVisible(false);
            sprites.Clear();
            fills.Clear();
        }

        public override void Removed(object scene)
        {
            Unregister();
            base.Removed(scene);
        }

        public override void SceneEnd(object scene)
        {
            base.SceneEnd(scene);
            Unregister();
        }

        protected bool DispatchCollision(SpinnerCollisionMode mode)
        {
            switch (mode)
            {
                case SpinnerCollisionMode.PassThrough: return true;
                case SpinnerCollisionMode.Shatter:
                    Destroy();
                    return true;
                case SpinnerCollisionMode.ShatterGroup:
                    foreach (object entity in ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntities(
                                 typeof(CustomSpinner)))
                        if (entity is CustomSpinner spinner && spinner.AttachGroup == AttachGroup)
                            spinner.Destroy();
                    return true;
                default: return false;
            }
        }

        protected override void OnPlayer(RoomPlayerSession player)
        {
            if (!DispatchCollision(player.DashAttacking ? DashThrough : PlayerCollisionMode)) base.OnPlayer(player);
        }

        protected override void OnHoldable(HoldableState holdable)
        {
            if (HoldableCollisionMode == SpinnerCollisionMode.PassThrough) base.OnHoldable(holdable);
            else if (HoldableCollisionMode == SpinnerCollisionMode.Kill)
                throw new Exception("Kill mode not supported for holdables!");
            else DispatchCollision(HoldableCollisionMode);
        }
    }
}
