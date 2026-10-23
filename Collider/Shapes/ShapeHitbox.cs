using System;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Geometry;
using Celeste.Core.Rendering;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.LinearAlgebra;
using Celeste.Core.Scheduling;
using Celeste.Graphics.Geometry.TwoD.Camera;
using Celeste.Graphics.Rendering;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Shapes;
using Celeste.Mods.FrostHelper.Runtime.Rendering;

namespace Celeste.Mods.FrostHelper.Collider.Shapes
{
    [Celeste.Core.Reflection.Runtime.RuntimeTypeAlias("FrostHelper.Colliders.ShapeHitbox")]
    public sealed class ShapeHitbox : ColliderListGeometry
    {
        public readonly Vector2[] Points;
        public bool Fill = true;
        private IntRect cullRectangle;
        private readonly HitboxColliderGeometry collisionRect = new(0, 0);
        private float width, height, top, left, right, bottom;

        public override float Width
        {
            get => width;
            set => throw new NotImplementedException();
        }

        public override float Height
        {
            get => height;
            set => throw new NotImplementedException();
        }

        public override float Top
        {
            get => top - (Entity?.Position.Y ?? 0f);
            set => throw new NotImplementedException();
        }

        public override float Bottom
        {
            get => bottom - (Entity?.Position.Y ?? 0f);
            set => throw new NotImplementedException();
        }

        public override float Left
        {
            get => left - (Entity?.Position.X ?? 0f);
            set => throw new NotImplementedException();
        }

        public override float Right
        {
            get => right - (Entity?.Position.X ?? 0f);
            set => throw new NotImplementedException();
        }

        public ShapeHitbox(Vector2[] points) : base(Array.Empty<ColliderGeometry>())
        {
            Points = points;
            OnChanged();
        }

        public void OnChanged()
        {
            bottom = top = Points[0].Y;
            left = right = Points[0].X;
            for (int i = 1; i < Points.Length; i++)
            {
                var point = Points[i];
                if (point.Y > bottom) bottom = point.Y;
                if (point.Y < top) top = point.Y;
                if (point.X < left) left = point.X;
                if (point.X > right) right = point.X;
            }

            width = Math.Abs(right - left);
            height = Math.Abs(bottom - top);
            cullRectangle = new IntRect((int)left - 1, (int)top - 1, (int)width + 2, (int)height + 2);
        }

        private IntRect GetCullRectangle()
        {
            var position = Entity?.Position ?? Vector2.Zero;
            return new IntRect(cullRectangle.X + (int)position.X, cullRectangle.Y + (int)position.Y,
                cullRectangle.Width, cullRectangle.Height);
        }

        public override void Added(ISourceEntityCollider entity)
        {
            base.Added(entity);
            var position = entity.Position;
            for (int i = 0; i < Points.Length; i++) Points[i] = SourceVector2Math.Subtract(Points[i], position);
            OnChanged();
        }

        public override ColliderGeometry Clone() => new ShapeHitbox(Points);
        public override bool Collide(Vector2 point) => Collide(new IntRect((int)point.X, (int)point.Y, 1, 1));

        private bool CollideAt(IntRect rect, Vector2 position)
        {
            var cull = new IntRect(cullRectangle.X + (int)position.X, cullRectangle.Y + (int)position.Y,
                cullRectangle.Width, cullRectangle.Height);
            if (!cull.Intersects(rect)) return false;
            collisionRect.Position = new Vector2(rect.Left, rect.Top);
            collisionRect.Width = rect.Width;
            collisionRect.Height = rect.Height;
            for (int i = 0; i < Points.Length - 1; i++)
                if (collisionRect.Collide(
                        SourceVector2Math.Add(Points[i], position), SourceVector2Math.Add(Points[i + 1], position)))
                    return true;
            return Fill && collisionRect.Collide(
                SourceVector2Math.Add(Points[0], position), SourceVector2Math.Add(Points[Points.Length - 1], position));
        }

        public override bool Collide(IntRect rect) => CollideAt(rect, Entity?.Position ?? Vector2.Zero);
        public override bool Collide(Vector2 from, Vector2 to) => throw new NotImplementedException();

        public override bool Collide(HitboxColliderGeometry box) => Collide(new IntRect((int)box.AbsoluteX,
            (int)box.AbsoluteY, (int)box.Width, (int)box.Height));

        public override bool Collide(GridColliderGeometry grid) => throw new NotImplementedException();
        public override bool Collide(CircleColliderGeometry circle) => throw new NotImplementedException();

        public override bool Collide(ColliderListGeometry list)
        {
            foreach (var child in list.colliders)
                if (child.Collide(this))
                    return true;
            return false;
        }

        public override bool CollideHitbox(Bounds box) =>
            Collide(new IntRect((int)box.X, (int)box.Y, (int)box.Width, (int)box.Height));

        public override bool Overlaps(Bounds box, Vector2 entityPosition) =>
            CollideAt(new IntRect((int)box.X, (int)box.Y, (int)box.Width, (int)box.Height), entityPosition);

        public override void Render(object camera, PackedColor color, ISourceRenderContext context)
        {
            var cull = GetCullRectangle();
            if (!FrostCameraCull.IsRectangleVisible(cull.X, cull.Y, cull.Width, cull.Height, 4f, (CameraState)camera))
                return;
            var position = Entity?.Position ?? Vector2.Zero;
            var scalar =
                ((ISourceVectorEnvironment)((ISourceEngineContextScene)((Celeste.Scene.Entities.SceneEntityState)Entity)
                    .Scene).EngineContext).VectorArithmetic;
            for (int i = 0; i < Points.Length - 1; i++)
                SourcePrimitiveDrawing.Line(context, Entity,
                    SourceVector2Math.Add(Points[i], position), SourceVector2Math.Add(Points[i + 1], position), color,
                    scalar);
            SourcePrimitiveDrawing.Line(context, Entity, SourceVector2Math.Add(Points[0], position),
                SourceVector2Math.Add(Points[Points.Length - 1], position), color, scalar);
        }
    }
}
