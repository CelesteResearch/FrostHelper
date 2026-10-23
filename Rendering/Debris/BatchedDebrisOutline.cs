using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Scheduling;
using Celeste.Graphics.Animation;
using Celeste.Scene.Tracking;

namespace Celeste.Mods.FrostHelper.Rendering.Debris
{
    internal sealed class BatchedDebrisOutline : SourceComponentState
    {
        private readonly ISourceEngineContext engine;
        private readonly PackedColor color;
        private BatchedDebrisOutlineRenderer renderer;
        internal SpriteImage Image { get; }

        internal BatchedDebrisOutline(SpriteImage image, PackedColor color, ISourceEngineContext engine) : base(false,
            false)
        {
            Image = image;
            this.color = color;
            this.engine = engine;
        }

        public override void EntityAwake()
        {
            base.EntityAwake();
            var state = ((ISourceScene)Scene).SourceScene;
            var entity = (Celeste.Scene.Entities.SceneEntityState)Entity;
            foreach (var actual in state.Entities)
                if (actual is BatchedDebrisOutlineRenderer existing && existing.Depth == entity.Depth &&
                    existing.Color.Equals(color))
                {
                    renderer = existing;
                    break;
                }

            if (renderer == null)
                foreach (var actual in state.GetToAdd())
                    if (actual is BatchedDebrisOutlineRenderer pending && pending.Depth == entity.Depth &&
                        pending.Color.Equals(color))
                    {
                        renderer = pending;
                        break;
                    }

            if (renderer == null)
            {
                renderer = new BatchedDebrisOutlineRenderer(engine, entity.Depth, color);
                state.QueueSourceEntity(renderer);
            }

            renderer.Images.Add(this);
        }

        private void Detach()
        {
            renderer?.Images.Remove(this);
            renderer = null;
        }

        public override void EntityRemoved(object scene)
        {
            base.EntityRemoved(scene);
            Detach();
        }

        public override void Removed(object entity)
        {
            base.Removed(entity);
            Detach();
        }
    }
}
