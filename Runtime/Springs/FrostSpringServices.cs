using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using Celeste.Core.Scheduling;
using Celeste.Core.Reflection.Interop;
using Celeste.Core.Components;
using Celeste.Scene.Components.Oscillation;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Interaction.Springs;
using Celeste.Game.Gameplay.Hazards.Puffer;
using Celeste.Mods.FrostHelper.Color;
using Celeste.Mods.FrostHelper.Entities.Springs;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Interaction.Springs;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;

namespace Celeste.Mods.FrostHelper.Runtime.Springs
{
    public sealed class FrostSpringServices : IDisposable
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, FrostSpringServices> Owners = new();

        public static FrostSpringServices For(ISourceEngineContext engine) => Owners.TryGetValue(engine, out var owner)
            ? owner
            : throw new InvalidOperationException("Frost spring services are not loaded for this engine.");

        private readonly ISourceEngineContext engine;
        private readonly List<IDisposable> hooks = new();
        private readonly IDisposable gravityLease;
        private bool disposed;
        public ColorReader Colors { get; }
        public FrostSpringGravity Gravity { get; } = new();
        private readonly FrostSpringCommunal communal = new();
        private readonly IDisposable communalLease;
        public Func<int> DreamTunnelState => communal.GetDreamTunnelDashState;
        private readonly Func<float> readNoClimb;
        private readonly Action<float> writeNoClimb;

        public float NoClimbTimer
        {
            get => readNoClimb();
            set => writeNoClimb(value);
        }

        public FrostSpringServices(ISourceEngineContext engine, ColorReader colors, Func<float> readNoClimb,
            Action<float> writeNoClimb)
        {
            this.engine = engine;
            Colors = colors;
            this.readNoClimb = readNoClimb;
            this.writeNoClimb = writeNoClimb;
            gravityLease = ((ISourceModInteropEnvironment)engine).ModInterop.RegisterOwned(
                new SourceModInteropActualTypeBinding(typeof(FrostSpringGravity), Gravity,
                    exportedMethods: Array.Empty<MethodInfo>()));
            communalLease = ((ISourceModInteropEnvironment)engine).ModInterop.RegisterOwned(
                new SourceModInteropActualTypeBinding(typeof(FrostSpringCommunal), communal,
                    exportedMethods: Array.Empty<MethodInfo>()));
            ((ISourceModInteropEnvironment)engine).ModInterop.ImportsRefreshed += OnImportsRefreshed;
            OnImportsRefreshed();
            Owners.Add(engine, this);
        }

        private void OnImportsRefreshed() => Gravity.RegisterModSupportBlacklist?.Invoke("FrostHelper");

        public void EnsureHooks()
        {
            if (hooks.Count != 0) return;
            hooks.Add(new ILHook(
                typeof(OriginalSpringState).GetMethod("BounceAnimate", BindingFlags.NonPublic | BindingFlags.Instance),
                il =>
                {
                    var cursor = new ILCursor(il);
                    cursor.Emit(OpCodes.Ldarg_0);
                    cursor.EmitDelegate<Action<OriginalSpringState>>(spring =>
                    {
                        if (spring is CustomSpring custom && ReferenceEquals(custom.Services, this))
                            custom.BeforeBounce();
                    });
                    while (cursor.TryGotoNext(MoveType.After, i => i.MatchLdstr("event:/game/general/spring")))
                    {
                        cursor.Emit(OpCodes.Ldarg_0);
                        cursor.EmitDelegate<Func<string, OriginalSpringState, string>>((original, spring) =>
                            spring is CustomSpring custom && ReferenceEquals(custom.Services, this)
                                ? custom.BounceSfx
                                : original);
                    }
                }));
            hooks.Add(new Hook(typeof(OriginalPufferState).GetMethod(nameof(OriginalPufferState.HitSpring)),
                (PufferHook)OnPuffer));
            hooks.Add(new Hook(
                typeof(RoomPlayerWorld).GetMethod(nameof(RoomPlayerWorld.ClimbCheck),
                    new[] { typeof(int), typeof(int) }),
                (ClimbHook)OnClimb));
            hooks.Add(new Hook(typeof(OriginalLevelSceneRoot).GetMethod(nameof(OriginalLevelSceneRoot.Update)),
                (LevelHook)OnUpdate));
        }

        private delegate bool PufferOriginal(OriginalPufferState puffer, ISourceSpringEntity spring);

        private delegate bool PufferHook(PufferOriginal original, OriginalPufferState puffer,
            ISourceSpringEntity spring);

        private delegate bool ClimbOriginal(RoomPlayerWorld world, int direction, int yOffset);

        private delegate bool ClimbHook(ClimbOriginal original, RoomPlayerWorld world, int direction, int yOffset);

        private delegate void LevelOriginal(OriginalLevelSceneRoot level);

        private delegate void LevelHook(LevelOriginal original, OriginalLevelSceneRoot level);

        private static readonly Action<OriginalPufferState, bool, bool> Alert =
            (Action<OriginalPufferState, bool, bool>)typeof(OriginalPufferState)
                .GetMethod("Alert", BindingFlags.Instance | BindingFlags.NonPublic)
                .CreateDelegate(typeof(Action<OriginalPufferState, bool, bool>));

        private static readonly Func<OriginalPufferState, WigglerComponent> BounceWiggler = GetWiggler();

        private static Func<OriginalPufferState, WigglerComponent> GetWiggler()
        {
            var p = Expression.Parameter(typeof(OriginalPufferState));
            return Expression
                .Lambda<Func<OriginalPufferState, WigglerComponent>>(Expression.Field(p, "bounceWiggler"), p)
                .Compile();
        }

        private bool OnPuffer(PufferOriginal original, OriginalPufferState puffer, ISourceSpringEntity spring)
        {
            if (spring is not CustomSpring custom || !ReferenceEquals(custom.Services, this) ||
                custom.Orientation != CustomSpring.CustomOrientations.Ceiling) return original(puffer, spring);
            if (puffer.HitSpeed.Y > 0) return false;
            puffer.LaunchFromHit(224f * System.Numerics.Vector2.UnitY);
            puffer.MoveTowardsX(custom.CenterX, 4f);
            BounceWiggler(puffer).Start();
            Alert(puffer, true, false);
            return true;
        }

        private bool OnClimb(ClimbOriginal original, RoomPlayerWorld world, int direction, int offset)
            => ReferenceEquals(world.EngineContext, engine) && NoClimbTimer > 0
                ? false
                : original(world, direction, offset);

        private void OnUpdate(LevelOriginal original, OriginalLevelSceneRoot level)
        {
            original(level);
            if (ReferenceEquals(level.EngineContext, engine)) NoClimbTimer -= engine.DeltaTime;
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            for (int i = hooks.Count - 1; i >= 0; i--) hooks[i].Dispose();
            hooks.Clear();
            ((ISourceModInteropEnvironment)engine).ModInterop.ImportsRefreshed -= OnImportsRefreshed;
            communalLease.Dispose();
            gravityLease.Dispose();
            Owners.Remove(engine);
        }
    }
}
