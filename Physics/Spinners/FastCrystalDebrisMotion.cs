using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Geometry;
using Celeste.Physics.Collision;
using Celeste.Simulation.Gameplay.Platforms.Collision;

namespace Celeste.Mods.FrostHelper.Physics.Spinners
{
    internal sealed class FastCrystalDebrisMotion : IPixelPlatformSource<CollisionPlatform>
    {
        private readonly PixelActor actor;
        private readonly List<object> broadest = new();
        private readonly List<object> solids = new();
        private readonly List<object> jumpThrus = new();
        private readonly PixelCollisionWorld<CollisionPlatform> collisionWorld;
        private readonly SourceCollisionHandler horizontalCapture;
        private SourceCollisionHandler horizontalCallback;
        private int horizontalMoved;

        internal ICollisionWorld CollisionWorld => collisionWorld;

        internal FastCrystalDebrisMotion(PixelActor actor)
        {
            this.actor = actor;
            horizontalCapture = CaptureHorizontalCollision;
            collisionWorld = new PixelCollisionWorld<CollisionPlatform> { PlatformSource = this };
        }

        List<object> IPixelPlatformSource<CollisionPlatform>.GetSolids() => solids;
        List<object> IPixelPlatformSource<CollisionPlatform>.GetJumpThrus() => jumpThrus;

        private static void Filter(List<object> source, IntRect rectangle, List<object> result)
        {
            foreach (var actual in source)
            {
                var entity = (ISourceEntityCollider)actual;
                if (entity.Collidable && entity.Collider != null && entity.Collider.Collide(rectangle))
                    result.Add(actual);
            }
        }

        private static IntRect Moved(IntRect rectangle, int x, int y)
            => new(rectangle.X + x, rectangle.Y + y, rectangle.Width, rectangle.Height);

        private static IntRect Merge(IntRect left, IntRect right)
        {
            var x = Math.Min(left.Left, right.Left);
            var y = Math.Min(left.Top, right.Top);
            return new IntRect(x, y, Math.Max(left.Right, right.Right) - x, Math.Max(left.Bottom, right.Bottom) - y);
        }

        internal bool MoveBoth(PlatformCollisionWorld sourceWorld, Vector2 move,
            SourceCollisionHandler onHorizontal, SourceCollisionHandler onVertical)
        {
            // Actual level worlds publish tracker-ordered entities, not extracted bounds.
            var source = sourceWorld.PlatformSource;
            var rectangle = actor.SourceColliderOwner.Collider.Bounds;
            Filter(source.GetSolids(), Merge(rectangle, Moved(rectangle, (int)move.X, (int)move.Y)), broadest);
            var collision = MoveHorizontal(move.X, rectangle, onHorizontal, out var moved);
            // Source intentionally leaves moved=0 in the no-candidate fast branch.
            rectangle.X += moved;
            collision |= MoveVertical(move.Y, rectangle, source, onVertical);
            broadest.Clear();
            solids.Clear();
            jumpThrus.Clear();
            return collision;
        }

        private bool MoveHorizontal(float amount, IntRect rectangle, SourceCollisionHandler onCollision, out int moved)
        {
            moved = 0;
            actor.Remainder.X += amount;
            var pixels = (int)Math.Round(actor.Remainder.X, MidpointRounding.ToEven);
            if (pixels == 0) return false;
            actor.Remainder.X -= pixels;
            solids.Clear();
            Filter(broadest, Merge(rectangle, Moved(rectangle, pixels, 0)), solids);
            if (solids.Count == 0)
            {
                actor.Position.X += pixels;
                return false;
            }

            horizontalMoved = pixels;
            horizontalCallback = onCollision;
            var collision = actor.MoveHExact(pixels, horizontalCapture);
            moved = horizontalMoved;
            horizontalCallback = null;
            return collision;
        }

        private void CaptureHorizontalCollision(PixelCollision collision)
        {
            horizontalMoved = (int)collision.Moved.X;
            horizontalCallback?.Invoke(collision);
        }

        private bool MoveVertical(float amount, IntRect rectangle, IPixelPlatformSource<CollisionPlatform> source,
            SourceCollisionHandler onCollision)
        {
            actor.Remainder.Y += amount;
            var pixels = (int)Math.Round(actor.Remainder.Y, MidpointRounding.ToEven);
            if (pixels == 0) return false;
            actor.Remainder.Y -= pixels;
            solids.Clear();
            var query = Merge(rectangle, Moved(rectangle, 0, pixels));
            Filter(broadest, query, solids);
            if (pixels > 0)
            {
                jumpThrus.Clear();
                Filter(source.GetJumpThrus(), query, jumpThrus);
            }

            if (solids.Count == 0 && jumpThrus.Count == 0)
            {
                actor.Position.Y += pixels;
                return false;
            }

            return actor.MoveVExact(pixels, onCollision);
        }
    }
}
