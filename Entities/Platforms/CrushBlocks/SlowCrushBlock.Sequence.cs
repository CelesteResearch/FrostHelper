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
        private IEnumerator AttackSequence()
        {
            ((IOriginalGameInputProcessEnvironment)engine).InputSource.Rumble(RumbleStrength.Strong,
                RumbleLength.Medium);
            StartShaking(0.4f);
            yield return 0.4f;
            if (!chillOut)
            {
                canActivate = true;
            }

            StopPlayerRunIntoAnimation = false;
            bool slowing = false;
            float speed = 0f;
            while (true)
            {
                if (!chillOut)
                {
                    speed = ScalarMath.Approach(speed, CrushSpeed, CrushAccel * engine.DeltaTime,
                        ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
                }
                else if (slowing || SourceTrackedEntityCollision.CheckAt(Tracker, "Celeste.SolidTiles", this,
                             SourceVector2Math.Add(Position, SourceVector2Math.Multiply(crushDir, 256f))))
                {
                    speed = ScalarMath.Approach(speed, 12f, CrushAccel * engine.DeltaTime * 0.25f,
                        ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
                    if (!slowing)
                    {
                        slowing = true;
                        SourceAlarmState.Set(engine.AlarmPool, OwnedComponents, 0.5f, delegate
                        {
                            face.Play("hurt");
                            StopMoveLoopForSlowdown();
                            TurnOffImages();
                        });
                    }
                }
                else
                {
                    speed = ScalarMath.Approach(speed, CrushSpeed, CrushAccel * engine.DeltaTime,
                        ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
                }

                bool flag = ((crushDir.X == 0f)
                    ? MoveVCheck(speed * crushDir.Y * engine.DeltaTime)
                    : MoveHCheck(speed * crushDir.X * engine.DeltaTime));
                if (flag)
                {
                    break;
                }

                if (((ISourceScene)Scene).SourceScene.Clock.OnInterval(0.02f, engine))
                {
                    Vector2 position;
                    float direction;
                    if (crushDir == Vector2.UnitX)
                    {
                        position = new Vector2(base.Left + 1f,
                            SourceRandomMath.Range(((ISourceRandomEnvironment)engine).Random, base.Top + 3f,
                                base.Bottom - 3f));
                        direction = (float)Math.PI;
                    }
                    else if (crushDir == SourceVector2Math.Negate(Vector2.UnitX))
                    {
                        position = new Vector2(base.Right - 1f,
                            SourceRandomMath.Range(((ISourceRandomEnvironment)engine).Random, base.Top + 3f,
                                base.Bottom - 3f));
                        direction = 0f;
                    }
                    else if (crushDir == Vector2.UnitY)
                    {
                        position = new Vector2(
                            SourceRandomMath.Range(((ISourceRandomEnvironment)engine).Random, base.Left + 3f,
                                base.Right - 3f), base.Top + 1f);
                        direction = -(float)Math.PI / 2f;
                    }
                    else
                    {
                        position = new Vector2(
                            SourceRandomMath.Range(((ISourceRandomEnvironment)engine).Random, base.Left + 3f,
                                base.Right - 3f), base.Bottom - 1f);
                        direction = (float)Math.PI / 2f;
                    }

                    level.Particles.Emit(P_Crushing, position, direction);
                }

                yield return null;
            }

            OriginalFallingBlockState fallingBlock =
                SourceTrackedEntityCollision.FirstAt<OriginalFallingBlockState>(Tracker, "Celeste.FallingBlock", this,
                    SourceVector2Math.Add(Position, crushDir));
            if (fallingBlock != null)
            {
                fallingBlock.Triggered = true;
            }

            if (crushDir == SourceVector2Math.Negate(Vector2.UnitX))
            {
                Vector2 vector = new Vector2(0f, 2f);
                for (int num = 0; (float)num < base.Height / 8f; num++)
                {
                    Vector2 vector2 = new Vector2(base.Left - 1f, base.Top + 4f + (float)(num * 8));
                    if (!SourceSceneCollision.CheckPoint(Tracker, "Celeste.Water", vector2) &&
                        SourceSceneCollision.CheckPoint(Tracker, "Celeste.Solid", vector2))
                    {
                        ((OriginalLevelSceneRoot)Scene).ParticlesFG.Emit(P_Impact,
                            SourceVector2Math.Add(vector2, vector),
                            0f);
                        ((OriginalLevelSceneRoot)Scene).ParticlesFG.Emit(P_Impact,
                            SourceVector2Math.Subtract(vector2, vector), 0f);
                    }
                }
            }
            else if (crushDir == Vector2.UnitX)
            {
                Vector2 vector3 = new Vector2(0f, 2f);
                for (int num2 = 0; (float)num2 < base.Height / 8f; num2++)
                {
                    Vector2 vector4 = new Vector2(base.Right + 1f, base.Top + 4f + (float)(num2 * 8));
                    if (!SourceSceneCollision.CheckPoint(Tracker, "Celeste.Water", vector4) &&
                        SourceSceneCollision.CheckPoint(Tracker, "Celeste.Solid", vector4))
                    {
                        ((OriginalLevelSceneRoot)Scene).ParticlesFG.Emit(P_Impact,
                            SourceVector2Math.Add(vector4, vector3),
                            (float)Math.PI);
                        ((OriginalLevelSceneRoot)Scene).ParticlesFG.Emit(P_Impact,
                            SourceVector2Math.Subtract(vector4, vector3), (float)Math.PI);
                    }
                }
            }
            else if (crushDir == SourceVector2Math.Negate(Vector2.UnitY))
            {
                Vector2 vector5 = new Vector2(2f, 0f);
                for (int num3 = 0; (float)num3 < base.Width / 8f; num3++)
                {
                    Vector2 vector6 = new Vector2(base.Left + 4f + (float)(num3 * 8), base.Top - 1f);
                    if (!SourceSceneCollision.CheckPoint(Tracker, "Celeste.Water", vector6) &&
                        SourceSceneCollision.CheckPoint(Tracker, "Celeste.Solid", vector6))
                    {
                        ((OriginalLevelSceneRoot)Scene).ParticlesFG.Emit(P_Impact,
                            SourceVector2Math.Add(vector6, vector5),
                            (float)Math.PI / 2f);
                        ((OriginalLevelSceneRoot)Scene).ParticlesFG.Emit(P_Impact,
                            SourceVector2Math.Subtract(vector6, vector5), (float)Math.PI / 2f);
                    }
                }
            }
            else if (crushDir == Vector2.UnitY)
            {
                Vector2 vector7 = new Vector2(2f, 0f);
                for (int num4 = 0; (float)num4 < base.Width / 8f; num4++)
                {
                    Vector2 vector8 = new Vector2(base.Left + 4f + (float)(num4 * 8), base.Bottom + 1f);
                    if (!SourceSceneCollision.CheckPoint(Tracker, "Celeste.Water", vector8) &&
                        SourceSceneCollision.CheckPoint(Tracker, "Celeste.Solid", vector8))
                    {
                        ((OriginalLevelSceneRoot)Scene).ParticlesFG.Emit(P_Impact,
                            SourceVector2Math.Add(vector8, vector7),
                            -(float)Math.PI / 2f);
                        ((OriginalLevelSceneRoot)Scene).ParticlesFG.Emit(P_Impact,
                            SourceVector2Math.Subtract(vector8, vector7), -(float)Math.PI / 2f);
                    }
                }
            }

            ((ISourceSpatialAudioEnvironment)engine).SpatialAudio.Play(
                ResolveSoundPath("event:/game/06_reflection/crushblock_impact"),
                base.Center);
            level.DirectionalShake(crushDir);
            ((IOriginalGameInputProcessEnvironment)engine).InputSource.Rumble(RumbleStrength.Medium,
                RumbleLength.Medium);
            StartShaking(0.4f);
            StopPlayerRunIntoAnimation = true;
            SoundSourceComponentState sfx = currentMoveLoopSfx;
            currentMoveLoopSfx.Param("end", 1f);
            currentMoveLoopSfx = null;
            SourceAlarmState.Set(engine.AlarmPool, OwnedComponents, 0.5f, delegate { sfx.RemoveSelf(); });
            crushDir = Vector2.Zero;
            TurnOffImages();
            if (chillOut)
            {
                yield break;
            }

            face.Play("hurt");
            returnLoopSfx.Play(ResolveSoundPath("event:/game/06_reflection/crushblock_return_loop"));
            yield return 0.4f;
            speed = 0f;
            float waypointSfxDelay = 0f;
            if (returnStack.Count <= 0) returnStack.Add(new MoveState(ExactPosition, Vector2.Zero));
            while (returnStack.Count > 0)
            {
                yield return null;
                StopPlayerRunIntoAnimation = false;
                MoveState moveState = returnStack[returnStack.Count - 1];
                speed = ScalarMath.Approach(speed, ReturnSpeed, ReturnAccel * engine.DeltaTime,
                    ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
                waypointSfxDelay -= engine.DeltaTime;
                if (moveState.Direction.X != 0f)
                {
                    MoveTowardsX(moveState.From.X, speed * engine.DeltaTime);
                }

                if (moveState.Direction.Y != 0f)
                {
                    MoveTowardsY(moveState.From.Y, speed * engine.DeltaTime);
                }

                if ((moveState.Direction.X != 0f && base.ExactPosition.X != moveState.From.X) ||
                    (moveState.Direction.Y != 0f && base.ExactPosition.Y != moveState.From.Y))
                {
                    continue;
                }

                speed = 0f;
                returnStack.RemoveAt(returnStack.Count - 1);
                StopPlayerRunIntoAnimation = true;
                if (returnStack.Count <= 0)
                {
                    face.Play("idle");
                    if (returnLoopSfx.Playing) returnLoopSfx.Stop();
                    if (waypointSfxDelay <= 0f)
                    {
                        ((ISourceSpatialAudioEnvironment)engine).SpatialAudio.Play(
                            ResolveSoundPath("event:/game/06_reflection/crushblock_rest"), base.Center);
                    }
                }
                else if (waypointSfxDelay <= 0f)
                {
                    ((ISourceSpatialAudioEnvironment)engine).SpatialAudio.Play(
                        ResolveSoundPath("event:/game/06_reflection/crushblock_rest_waypoint"), base.Center);
                }

                waypointSfxDelay = 0.1f;
                StartShaking(0.2f);
                yield return 0.2f;
            }
        }
    }
}
