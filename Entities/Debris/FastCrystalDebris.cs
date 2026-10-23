using System;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Geometry;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Randomness;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Levels;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content.Atlas;
using Celeste.Mods.FrostHelper.Physics.Spinners;
using Celeste.Mods.FrostHelper.Rendering.Debris;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Simulation.Gameplay.Effects.Debris;
using Celeste.Simulation.Gameplay.Platforms.Collision;
using Celeste.Simulation.Presentation.Particles;
using Celeste.Simulation.Presentation.Particles.Definitions;

namespace Celeste.Mods.FrostHelper.Entities.Debris
{
    public sealed class FastCrystalDebris : SceneEntityState, ISourceActorCollisionWorldProvider
    {
        private readonly ISourceEngineContext engine;
        private readonly SpriteImage image;
        private readonly FastCrystalDebrisMotion movement;
        private readonly SourceCollisionHandler collideH;
        private readonly SourceCollisionHandler collideV;
        private float percent;
        private float duration;
        private Vector2 speed;
        private bool bossShatter;
        private IntRect levelBounds;
        public PixelActor Body { get; }

        public override Vector2 Position
        {
            get => Body.Position;
            set => Body.Position = value;
        }

        ICollisionWorld ISourceActorCollisionWorldProvider.CollisionWorld => movement.CollisionWorld;

        public FastCrystalDebris(AtlasData atlas, ISourceEngineContext engine)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration)
        {
            this.engine = engine;
            Body = new PixelActor(null, Vector2.Zero) { SourceColliderOwner = this };
            Depth = -9990;
            Collider = new HitboxColliderGeometry(2f, 2f, -1f, -1f);
            collideH = OnCollideH;
            collideV = OnCollideV;
            image = new SpriteImage(atlas.Entries["particles/shard"]);
            image.CenterOrigin();
            image.SetRawEntity(this);
            Add(new BatchedDebrisOutline(image, PackedColor.Black, engine));
            Visible = false;
            movement = new FastCrystalDebrisMotion(Body);
        }

        private void Init(Vector2 position, PackedColor color, bool boss)
        {
            Position = position;
            image.Color = color;
            image.Scale = Vector2.One;
            percent = 0f;
            CrystalDebrisPhysics.Initialize(((ISourceRandomEnvironment)engine).Random, boss, 20f, out duration,
                out speed);
            bossShatter = boss;
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            levelBounds = ((OriginalLevelSceneRoot)scene).Bounds;
            levelBounds.Inflate(8, 8);
            if (CollidesWithSolid()) RemoveSelf();
        }

        private bool CollidesWithSolid()
            => ((ISourceActorCollisionWorld)((ISourcePlatformCollisionScene)Scene).PlatformWorld)
                .FirstSolidAt(Body, Position) != null;

        private bool ContainsLevelBounds(Vector2 value)
        {
            // Fixed Frost RectangleExt's Vector128 path, expressed without a host SIMD dependency.
            return levelBounds.X - value.X <= 0f && levelBounds.Y - value.Y <= 0f &&
                   value.X - levelBounds.X - levelBounds.Width <= -1f &&
                   value.Y - levelBounds.Y - levelBounds.Height <= -1f;
        }

        public override void Update()
        {
            speed.X = Math.Min(Math.Max(speed.X, -100000f), 100000f);
            speed.Y = Math.Min(Math.Max(speed.Y, -100000f), 100000f);
            // Original Frost entity deliberately performs no Actor/components update.
            if (percent > 1f || !ContainsLevelBounds(Position))
            {
                RemoveSelf();
                return;
            }

            if (!CrystalDebrisPhysics.Advance(ref speed, ref percent, duration, bossShatter, engine.DeltaTime,
                    ((ISourceVectorEnvironment)engine).VectorArithmetic,
                    ((ISourceExtremaEnvironment)engine).ExtremaArithmetic, out var scale, out var rotation))
            {
                RemoveSelf();
                return;
            }

            if (rotation.HasValue) image.Rotation = rotation.Value;
            image.Scale = scale;
            var previous = Position;
            movement.MoveBoth(((ISourcePlatformCollisionScene)Scene).PlatformWorld,
                speed * engine.DeltaTime, collideH, collideV);
            if (Position == previous && CollidesWithSolid())
            {
                RemoveSelf();
                return;
            }

            if (((ISourceScene)Scene).SourceScene.Clock.OnInterval(.05f, engine))
                ((ISourceForegroundParticleScene)Scene).ForegroundParticles.Emit(
                    ((ICrystalDebrisParticleEnvironment)engine).Dust, Position);
        }

        private void OnCollideH(PixelCollision collision) => CrystalDebrisPhysics.BounceHorizontal(ref speed);

        private void OnCollideV(PixelCollision collision)
        {
            if (!CrystalDebrisPhysics.BounceVertical(ref speed, bossShatter, ((ISourceRandomEnvironment)engine).Random))
                RemoveSelf();
        }

        public static void Burst(ISourceEngineContext engine, Vector2 position, PackedColor color,
            bool boss, int count = 1)
        {
            var random = ((ISourceRandomEnvironment)engine).Random;
            for (var i = 0; i < count; i++)
            {
                var piece = engine.EntityPools.Create<FastCrystalDebris>();
                var at = position + new Vector2(SourceRandomMath.Range(random, -4, 4),
                    SourceRandomMath.Range(random, -4, 4));
                piece.Init(at, color, boss);
                ((ISourceScene)engine.Frame.Scenes.CurrentScene).SourceScene.QueueSourceEntity(piece);
            }
        }
    }
}
