using System;
using System.Reflection.Emit;
using Celeste.Core.Rendering;
using Celeste.Simulation.Presentation.Backdrops.Runtime;

namespace Celeste.Mods.FrostHelper.Runtime.Stylegrounds
{
    public sealed class InvisibleStylegroundWrapper : BackdropPresentation
    {
        private static readonly Action<BackdropPresentation, ISourceBackdropScene> BaseUpdate = CreateBaseUpdate();
        public readonly BackdropPresentation Inner;

        public InvisibleStylegroundWrapper(BackdropPresentation inner)
        {
            Inner = inner;
            OnlyIfFlag = inner.OnlyIfFlag;
            OnlyIfNotFlag = inner.OnlyIfNotFlag;
            AlsoIfFlag = inner.AlsoIfFlag;
            Dreaming = inner.Dreaming;
            ExcludeFrom = inner.ExcludeFrom;
            OnlyIn = inner.OnlyIn;
            InstantIn = inner.InstantIn;
            InstantOut = inner.InstantOut;
            Tags = inner.Tags;
        }

        private static Action<BackdropPresentation, ISourceBackdropScene> CreateBaseUpdate()
        {
            var method = new DynamicMethod("FrostHelper.Wrapper.base_Update", typeof(void),
                new[] { typeof(BackdropPresentation), typeof(ISourceBackdropScene) },
                typeof(InvisibleStylegroundWrapper), true);
            var il = method.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, typeof(BackdropPresentation).GetMethod(nameof(Update)));
            il.Emit(OpCodes.Ret);
            return (Action<BackdropPresentation, ISourceBackdropScene>)method.CreateDelegate(
                typeof(Action<BackdropPresentation, ISourceBackdropScene>));
        }

        public override void Update(ISourceBackdropScene scene)
        {
            BaseUpdate(Inner, scene);
            Visible = Inner.Visible;
            if (Visible) Inner.Update(scene);
        }

        public override void Render(ISourceRenderContext context, ISourceBackdropScene scene) =>
            Inner.Render(context, scene);

        public override void BeforeRender(ISourceBackdropScene scene)
        {
            if (Visible) Inner.BeforeRender(scene);
        }

        public override void Ended(ISourceBackdropScene scene) => Inner.Ended(scene);
    }
}
