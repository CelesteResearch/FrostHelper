using System.Numerics;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Entities.Platforms.SwapBlocks.Motion;

namespace Celeste.Mods.FrostHelper.Entities.Platforms.SwapBlocks
{
    public partial class ToggleSwapBlock
    {
        private void OnDash(Vector2 direction)
        {
            Swapping = lerp < 1f;
            Audio.Stop(returnSfx, true);
            Audio.Stop(moveSfx, true);
            moveSfx = Audio.Play(MoveSFX, Center);
            target = (target == 1) ? 0 : 1;
            returnTimer = 0.8f;
            burst = (Scene as OriginalLevelSceneRoot)!.Displacement.AddBurst(Center, 0.2f, 0f, 16f, 1f, null, null);
            if (lerp >= 0.2f)
            {
                speed = maxForwardSpeed;
            }
            else
            {
                speed = Scalar.Lerp(maxForwardSpeed * 0.333f, maxForwardSpeed, lerp / 0.2f);
            }

            if (!Swapping)
            {
                Audio.Play(MoveEndSFX, Center);
                return;
            }
        }

        public override void Update()
        {
            base.Update();
            if (returnTimer > 0f)
            {
                returnTimer -= Engine.DeltaTime;
                if (returnTimer <= 0f)
                {
                    speed = 0f;
                }
            }

            float num = SwapBlockMotion.AdvanceProgress(new MotionState(this));
            if (start == end)
                lerp = target;

            if (lerp != num)
            {
                var motion = new MotionState(this);
                Vector2 vector = SwapBlockMotion.GetInitialLiftSpeed(motion);
                Vector2 position = Position;
                vector = SwapBlockMotion.AdjustLiftSpeed(motion, num, vector);
                if ( /*target == 1 &&*/ OnInterval(0.02f))
                {
                    MoveParticles(target switch
                    {
                        1 => end - start,
                        _ => start - end,
                    });
                }

                if (start != end)
                {
                    SwapBlockMotion.MoveToProgress(motion, vector);
                }

                if (position != Position)
                {
                    Audio.Position(moveSfx, Center);
                    Audio.Position(returnSfx, Center);
                    if (Position == start && target == 0)
                    {
                        ((IOriginalGameplayProcess)Engine).AudioSession.SetParameter(returnSfx, "end", 1f);
                        Audio.Play(MoveEndSFX, Center);
                    }
                    else if (Position == end && target == 1)
                    {
                        ((IOriginalGameplayProcess)Engine).AudioSession.SetParameter(moveSfx, "end", 1f);
                        Audio.Play(MoveEndSFX, Center);
                    }
                }
            }

            SwapBlockMotion.Complete(new MotionState(this));
        }

        private void MoveParticles(Vector2 normal)
        {
            if (!EmitParticles) return;
            int amount = SwapBlockMotion.GetMovementParticleGeometry(this, Engine, normal, ref particlesRemainder,
                out Vector2 position, out Vector2 range, out float direction);
            ((OriginalLevelSceneRoot)Scene).Particles.Emit(ParticleType, amount, position, range, direction);
        }
    }
}
