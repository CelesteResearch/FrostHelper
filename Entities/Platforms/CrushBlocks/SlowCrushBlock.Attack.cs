using Celeste.Core.Display;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Scheduling;
using Celeste.Core.Rendering;
using Celeste.Core.Geometry;
using Celeste.Core.Audio;
using Celeste.Core.Randomness;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Mathematics.LinearAlgebra;
using Celeste.Core.Components.Timing;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Display;
using Celeste.Graphics.Rendering;
using Celeste.Simulation;
using Celeste.Simulation.Audio.Sources;
using Celeste.Simulation.Presentation.Lighting;
using Celeste.Simulation.Presentation.Particles.Definitions;
using Celeste.Simulation.Gameplay.Environment.Water;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Player.Collision.Surfaces;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Game.Execution.Graphics;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Controls.Source;
using Celeste.Game.Gameplay.Platforms.FallingBlocks;

namespace Celeste.Mods.FrostHelper.Entities.Platforms.CrushBlocks
{
    public partial class SlowCrushBlock
    {
        private void TurnOffImages()
        {
            foreach (SpriteImage activeLeftImage in activeLeftImages)
            {
                activeLeftImage.Visible = false;
            }

            foreach (SpriteImage activeRightImage in activeRightImages)
            {
                activeRightImage.Visible = false;
            }

            foreach (SpriteImage activeTopImage in activeTopImages)
            {
                activeTopImage.Visible = false;
            }

            foreach (SpriteImage activeBottomImage in activeBottomImages)
            {
                activeBottomImage.Visible = false;
            }
        }

        private DashCollisionResult OnDashed(RoomPlayerSession player, Vector2 direction)
        {
            if (CanActivate(SourceVector2Math.Negate(direction)))
            {
                Attack(SourceVector2Math.Negate(direction));
                return DashCollisionResult.Rebound;
            }

            return DashCollisionResult.NormalCollision;
        }

        private bool CanActivate(Vector2 direction)
        {
            if (giant && direction.X <= 0f)
            {
                return false;
            }

            if (canActivate && crushDir != direction)
            {
                if (direction.X != 0f && !canMoveHorizontally)
                {
                    return false;
                }

                if (direction.Y != 0f && !canMoveVertically)
                {
                    return false;
                }

                return true;
            }

            return false;
        }

        private void Attack(Vector2 direction)
        {
            ((ISourceSpatialAudioEnvironment)engine).SpatialAudio.Play(
                ResolveSoundPath("event:/game/06_reflection/crushblock_activate"),
                base.Center);
            if (currentMoveLoopSfx != null)
            {
                currentMoveLoopSfx.Param("end", 1f);
                SoundSourceComponentState sfx = currentMoveLoopSfx;
                SourceAlarmState.Set(engine.AlarmPool, OwnedComponents, 0.5f, delegate { sfx.RemoveSelf(); });
            }

            OwnedComponents.Add(currentMoveLoopSfx = new SoundSourceComponentState(engine));
            currentMoveLoopSfx.Position =
                ((ISourceVectorEnvironment)engine).VectorArithmetic.Divide(new Vector2(base.Width, base.Height), 2f);
            currentMoveLoopSfx.Play(Sound("move_loop"));

            face.Play("hit");
            crushDir = direction;
            canActivate = false;
            attackCoroutine.Replace(AttackSequence());
            ClearRemainder();
            TurnOffImages();
            ActivateParticles(crushDir);
            if (crushDir.X < 0f)
            {
                foreach (SpriteImage activeLeftImage in activeLeftImages)
                {
                    activeLeftImage.Visible = true;
                }

                nextFaceDirection = "left";
            }
            else if (crushDir.X > 0f)
            {
                foreach (SpriteImage activeRightImage in activeRightImages)
                {
                    activeRightImage.Visible = true;
                }

                nextFaceDirection = "right";
            }
            else if (crushDir.Y < 0f)
            {
                foreach (SpriteImage activeTopImage in activeTopImages)
                {
                    activeTopImage.Visible = true;
                }

                nextFaceDirection = "up";
            }
            else if (crushDir.Y > 0f)
            {
                foreach (SpriteImage activeBottomImage in activeBottomImages)
                {
                    activeBottomImage.Visible = true;
                }

                nextFaceDirection = "down";
            }

            bool flag = !IsNoReturn;
            if (returnStack.Count > 0)
            {
                MoveState moveState = returnStack[returnStack.Count - 1];
                if (moveState.Direction == direction || moveState.Direction == SourceVector2Math.Negate(direction))
                {
                    flag = false;
                }
            }

            if (flag)
            {
                returnStack.Add(new MoveState(Position, crushDir));
            }
        }

        private void ActivateParticles(Vector2 dir)
        {
            float direction;
            Vector2 position;
            Vector2 positionRange;
            int num;
            if (dir == Vector2.UnitX)
            {
                direction = 0f;
                position = SourceVector2Math.Subtract(base.CenterRight, Vector2.UnitX);
                positionRange =
                    SourceVector2Math.Multiply(SourceVector2Math.Multiply(Vector2.UnitY, (base.Height - 2f)), 0.5f);
                num = (int)(base.Height / 8f) * 4;
            }
            else if (dir == SourceVector2Math.Negate(Vector2.UnitX))
            {
                direction = (float)Math.PI;
                position = SourceVector2Math.Add(base.CenterLeft, Vector2.UnitX);
                positionRange =
                    SourceVector2Math.Multiply(SourceVector2Math.Multiply(Vector2.UnitY, (base.Height - 2f)), 0.5f);
                num = (int)(base.Height / 8f) * 4;
            }
            else if (dir == Vector2.UnitY)
            {
                direction = (float)Math.PI / 2f;
                position = SourceVector2Math.Subtract(base.BottomCenter, Vector2.UnitY);
                positionRange =
                    SourceVector2Math.Multiply(SourceVector2Math.Multiply(Vector2.UnitX, (base.Width - 2f)), 0.5f);
                num = (int)(base.Width / 8f) * 4;
            }
            else
            {
                direction = -(float)Math.PI / 2f;
                position = SourceVector2Math.Add(base.TopCenter, Vector2.UnitY);
                positionRange =
                    SourceVector2Math.Multiply(SourceVector2Math.Multiply(Vector2.UnitX, (base.Width - 2f)), 0.5f);
                num = (int)(base.Width / 8f) * 4;
            }

            num += 2;
            level.Particles.Emit(P_Activate, num, position, positionRange, direction);
        }
    }
}
