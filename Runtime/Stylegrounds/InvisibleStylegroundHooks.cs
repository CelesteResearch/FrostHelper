using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Celeste.Core.Scheduling;
using Celeste.Simulation.Presentation.Backdrops.Parallax;
using Celeste.Simulation.Presentation.Backdrops.Runtime;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.FrostHelper.Runtime.Stylegrounds
{
    public sealed class InvisibleStylegroundHooks : IDisposable
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, InvisibleStylegroundHooks> Owners = new();
        private readonly ConditionalWeakTable<ParallaxBackdropPresentation, object> wrapped = new();
        private readonly ISourceEngineContext engine;
        private ILHook hook;
        private static readonly object Marker = new();

        private InvisibleStylegroundHooks(ISourceEngineContext engine) => this.engine = engine;

        public static InvisibleStylegroundHooks Load(ISourceEngineContext engine)
        {
            var owner = Owners.GetValue(engine, static e => new InvisibleStylegroundHooks(e));
            owner.hook ??=
                new ILHook(typeof(ParallaxBackdropPresentation).GetMethod(nameof(ParallaxBackdropPresentation.Update)),
                    owner.Apply);
            return owner;
        }

        public void Wrap(ParallaxBackdropPresentation parallax) => wrapped.GetValue(parallax, static _ => Marker);

        private bool ShouldUpdate(ParallaxBackdropPresentation parallax, ISourceBackdropScene scene) =>
            !ReferenceEquals(scene.EngineContext, engine) || parallax.Visible || !wrapped.TryGetValue(parallax, out _);

        private void Apply(ILContext il)
        {
            var cursor = new ILCursor(il);
            if (!cursor.TryGotoNext(MoveType.After,
                    i => i.MatchCall<BackdropPresentation>(nameof(BackdropPresentation.Update))))
                return;
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.EmitDelegate<Func<ParallaxBackdropPresentation, ISourceBackdropScene, bool>>(ShouldUpdate);
            var resume = cursor.DefineLabel();
            cursor.Emit(OpCodes.Brtrue, resume);
            cursor.Emit(OpCodes.Ret);
            cursor.MarkLabel(resume);
        }

        public static void Unload(ISourceEngineContext engine)
        {
            if (Owners.TryGetValue(engine, out var owner)) owner.Dispose();
        }

        public void Dispose()
        {
            hook?.Dispose();
            hook = null;
        }
    }
}
