using System.Numerics;
using Celeste.Core.Audio;
using Celeste.Core.Scheduling;
using Celeste.Game.Entities.Platforms.SwapBlocks.Motion;
using Celeste.Graphics.Animation;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Presentation.PostProcessing.Displacement;

namespace Celeste.Mods.FrostHelper.Entities.Platforms.SwapBlocks
{
    public partial class ToggleSwapBlock
    {
        private readonly struct MotionState(ToggleSwapBlock owner) : ISwapBlockMotionState
        {
            public ISourceEngineContext Engine => owner.Engine;
            public SourceSolidEntityState Solid => owner;
            public Vector2 Start => owner.start;
            public Vector2 End => owner.end;
            public float MaxForwardSpeed => owner.maxForwardSpeed;
            public float MaxBackwardSpeed => owner.maxBackwardSpeed;
            public Sprite MiddleRed => owner.middleRed;
            public Sprite MiddleGreen => owner.middleGreen;
            public ref bool Swapping => ref owner.Swapping;
            public ref int Target => ref owner.target;
            public ref float ReturnTimer => ref owner.returnTimer;
            public ref float Lerp => ref owner.lerp;
            public ref float Speed => ref owner.speed;
            public ref float RedAlpha => ref owner.redAlpha;
            public ref DisplacementBurst Burst => ref owner.burst;
            public ref ISourceAudioEventReference MoveSound => ref owner.moveSfx;
            public ref ISourceAudioEventReference ReturnSound => ref owner.returnSfx;
            public void EmitMovementParticles(Vector2 normal) => owner.MoveParticles(normal);
        }
    }
}
