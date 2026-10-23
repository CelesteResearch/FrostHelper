using System;
using System.Reflection;
using System.Reflection.Emit;
using Celeste.Core.Display;
using Celeste.Core.Scheduling;
using Celeste.Game.Presentation.Effects.Bloom;
using Celeste.Mods.FrostHelper.Color;
using Celeste.Mods.FrostHelper.Sessions;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.FrostHelper.Runtime.Lighting
{
    public sealed class FrostBloomColors : IDisposable
    {
        private static readonly Func<BloomRendererPresentation, IOriginalBloomProcess> RendererEngine =
            CreateRendererEngine();

        private readonly ISourceEngineContext engine;
        private readonly Func<FrostHelperSession> session;
        private ILHook hook;
        public ColorReader Colors { get; }
        public Func<PackedColor, PackedColor> ColorManipulator = c => c;

        public PackedColor Color
        {
            get => session().BloomColor;
            set => session().BloomColor = value;
        }

        public FrostBloomColors(ISourceEngineContext engine, ColorReader colors, Func<FrostHelperSession> session)
        {
            this.engine = engine;
            Colors = colors;
            this.session = session;
        }

        public void Load() => hook ??=
            new ILHook(typeof(BloomRendererPresentation).GetMethod(nameof(BloomRendererPresentation.Apply)), Patch);

        private static Func<BloomRendererPresentation, IOriginalBloomProcess> CreateRendererEngine()
        {
            var getter = new DynamicMethod("FrostBloomRendererEngine", typeof(IOriginalBloomProcess),
                new[] { typeof(BloomRendererPresentation) }, typeof(FrostBloomColors), true);
            var il = getter.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld,
                typeof(BloomRendererPresentation).GetField("engine", BindingFlags.NonPublic | BindingFlags.Instance));
            il.Emit(OpCodes.Ret);
            return (Func<BloomRendererPresentation, IOriginalBloomProcess>)getter.CreateDelegate(
                typeof(Func<BloomRendererPresentation, IOriginalBloomProcess>));
        }

        private PackedColor ChangeColor(PackedColor previous, BloomRendererPresentation renderer)
            => ReferenceEquals(RendererEngine(renderer), engine) ? ColorManipulator(Color) : previous;

        private void Patch(ILContext il)
        {
            var cursor = new ILCursor(il);
            int count = 0;
            while (cursor.TryGotoNext(MoveType.After, instruction => instruction.MatchCall<PackedColor>("get_White")))
            {
                cursor.Emit(Mono.Cecil.Cil.OpCodes.Ldarg_0);
                cursor.EmitDelegate<Func<PackedColor, BloomRendererPresentation, PackedColor>>(ChangeColor);
                count++;
            }

            if (count == 0) throw new InvalidOperationException("Frost bloom white consumer anchors missing");
        }

        public void Dispose()
        {
            hook?.Dispose();
            hook = null;
        }
    }
}
