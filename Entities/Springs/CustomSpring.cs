using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Interaction.Springs;
using Celeste.Game.Gameplay.Hazards.Puffer;
using Celeste.Game.Gameplay.Holdables.Gliders;
using Celeste.Game.Gameplay.Holdables.TheoCrystal;
using Celeste.Simulation.Gameplay.Actors.Foundation;
using Celeste.Graphics.Animation;
using Celeste.Scene.Entities;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Holdables;
using Celeste.Simulation.Gameplay.Hazards.Puffer;
using Celeste.Simulation.Gameplay.Interaction.Springs;
using Celeste.Simulation.Presentation.Particles.Definitions;
using Celeste.Game.Controls.Source;
using Celeste.Mods.FrostHelper.Runtime.Springs;

namespace Celeste.Mods.FrostHelper.Entities.Springs
{
    public partial class CustomSpring : OriginalSpringState
    {
        public readonly string BounceSfx;


        public enum CustomOrientations
        {
            Floor,
            WallLeft,
            WallRight,
            Ceiling
        }

        private float _inactiveTimer;

        public new CustomOrientations Orientation;

        private readonly bool _renderOutline;
        private Sprite Sprite => SpringSprite;

        internal readonly Vector2 SpeedMult;

        // 0, 1 - original
        // 2 - new gravity helper physics
        // 3 - communal helper dream tunnel support.
        // 4 - upside down spring holdable auto-support
        // 5 - break one-use springs if BounceAnimate is called, fixes the option for aushelper usages.
        private readonly int _version;

        private readonly bool _multiplyPlayerY;

        private readonly bool _oneUse;

        /// <summary>
        /// Whether the spring should always activate, regardless of the player's/holdable/fish speed.
        /// </summary>
        private readonly bool _alwaysActivate;

        private readonly SpringRecovery _recovery;

        private static readonly Dictionary<string, CustomOrientations> EntityDataNameToOrientation = new()
        {
            ["FrostHelper/SpringLeft"] = CustomOrientations.WallLeft,
            ["FrostHelper/SpringRight"] = CustomOrientations.WallRight,
            ["FrostHelper/SpringFloor"] = CustomOrientations.Floor,
            ["FrostHelper/SpringCeiling"] = CustomOrientations.Ceiling,
        };

        private static readonly Dictionary<CustomOrientations, SpringOrientation> CustomToRegularOrientation = new()
        {
            [CustomOrientations.WallLeft] = SpringOrientation.WallLeft,
            [CustomOrientations.WallRight] = SpringOrientation.WallRight,
            [CustomOrientations.Floor] = SpringOrientation.Floor,
            [CustomOrientations.Ceiling] = SpringOrientation.Floor,
        };

        private void NewOnCollide(RoomPlayerSession player)
        {
            if (player.Machine.State == 9 || !SpringPlayerCanUse)
                return;

            // Handle dream tunnel state same as dream dash and prevent collision.
            if (_version >= 3 && services.DreamTunnelState != null &&
                services.DreamTunnelState?.Invoke() is { } dreamTunnelState
                && player.Machine.State == dreamTunnelState)
            {
                return;
            }

            var prev = _recovery.SavePlayerData(player);

            if (!_recovery.CanUse(prev))
                return;

            if (_version == 0 && NewOnCollide_Version0(player, prev))
            {
                return;
            }

            switch (Orientation)
            {
                case CustomOrientations.Floor:
                case CustomOrientations.Ceiling:
                    // weird check here to fix buffered spring cancels
                    var realY = services.Gravity.InvertIfPlayerInverted(player.Data.Motion.Normal.Speed.Y);
                    if (!_alwaysActivate && (Orientation == CustomOrientations.Floor && realY < 0 ||
                                             Orientation == CustomOrientations.Ceiling &&
                                             (realY > 0 || realY == 0 && _inactiveTimer > 0)))
                    {
                        return;
                    }

                    var playerInverted = services.Gravity.IsPlayerInverted?.Invoke() ?? false;

                    AnimateSpringBounce();

                    if (playerInverted && Orientation == CustomOrientations.Floor)
                    {
                        services.Gravity.InvokeInvertedBounce(player, Top);
                        player.Data.Motion.Normal.Speed.Y *= SpeedMult.Y;
                    }
                    else if (!playerInverted && Orientation == CustomOrientations.Ceiling)
                    {
                        player.SuperBounce(Bottom + player.Height);
                        player.Data.Motion.Normal.Speed.Y *= -SpeedMult.Y;
                    }
                    else
                    {
                        player.SuperBounce(Orientation == CustomOrientations.Floor ? Top : Bottom);
                        player.Data.Motion.Normal.Speed.Y *= SpeedMult.Y;
                    }

                    player.Data.Motion.Normal.VariableJumpSpeed = player.Data.Motion.Normal.Speed.Y;

                    OnSuccessfulPlayerHit(player, prev);

                    if (playerInverted && Orientation == CustomOrientations.Floor ||
                        Orientation == CustomOrientations.Ceiling)
                    {
                        services.NoClimbTimer = 4f / 60f;
                        _inactiveTimer = 6f * SpringProcess.DeltaTime;
                    }

                    break;
                case CustomOrientations.WallLeft:
                    // If AlwaysActivate is enabled, we'll set the player's speed to the target direction,
                    // to make the early return in player.SideBounce never activate.
                    // Alternatively, player.SideBounce could be hooked to do this, but that's more work and overhead :p
                    if (_alwaysActivate)
                        player.Data.Motion.Normal.Speed.X = 1;

                    if (player.SideBounce(1, Right, CenterY))
                    {
                        AnimateSpringBounce();
                        player.Data.Motion.Normal.Speed *= SpeedMult;
                        if (_multiplyPlayerY)
                            player.Data.Motion.Normal.VariableJumpSpeed = player.Data.Motion.Normal.Speed.Y;
                        OnSuccessfulPlayerHit(player, prev);
                    }

                    break;
                case CustomOrientations.WallRight:
                    // read comment in case WallLeft
                    if (_alwaysActivate)
                        player.Data.Motion.Normal.Speed.X = -1;

                    if (player.SideBounce(-1, Left, CenterY))
                    {
                        player.Data.Motion.Normal.Speed *= SpeedMult;
                        if (_multiplyPlayerY)
                            player.Data.Motion.Normal.VariableJumpSpeed = player.Data.Motion.Normal.Speed.Y;
                        AnimateSpringBounce();

                        OnSuccessfulPlayerHit(player, prev);
                    }

                    break;
                default:
                    throw new Exception("Orientation not supported!");
            }
        }

        /// <summary>
        /// Old version of the NewOnCollide method before PR4. This has broken upside-down spring behaviour - it launches the player way too high.
        /// </summary>
        private bool NewOnCollide_Version0(RoomPlayerSession player, SpringRecovery.SavedPlayerData prev)
        {
            switch (Orientation)
            {
                case CustomOrientations.Floor:
                    if (_alwaysActivate || services.Gravity.InvertIfPlayerInverted(player.Data.Motion.Normal.Speed.Y) >=
                        0f)
                    {
                        AnimateSpringBounce();
                        services.Gravity.SuperBounce(player, Top);
                        player.Data.Motion.Normal.Speed.Y *= SpeedMult.Y;

                        OnSuccessfulPlayerHit(player, prev);
                    }

                    return true;
                case CustomOrientations.Ceiling:
                    // weird check here to fix buffered spring cancels
                    if (_alwaysActivate || (
                            services.Gravity.InvertIfPlayerInverted(player.Data.Motion.Normal.Speed.Y) < 0f
                            || (player.Data.Motion.Normal.Speed.Y == 0f && _inactiveTimer <= 0f)
                        ))
                    {
                        AnimateSpringBounce();

                        if (services.Gravity.IsPlayerInverted?.Invoke() ?? false)
                        {
                            player.SuperBounce(Bottom + player.Height);
                            player.Data.Motion.Normal.Speed.Y *= SpeedMult.Y;
                        }
                        else
                        {
                            player.SuperBounce(Bottom + player.Height);
                            player.Data.Motion.Normal.Speed.Y *= -SpeedMult.Y;
                        }

                        player.Data.Motion.Normal.VariableJumpSpeed = player.Data.Motion.Normal.Speed.Y;
                        OnSuccessfulPlayerHit(player, prev);

                        services.NoClimbTimer = 4f / 60f;
                        _inactiveTimer = 6f * SpringProcess.DeltaTime;
                    }

                    return true;
            }

            return false;
        }

        private bool CallHitSpring(HoldableState h)
        {
            // Holdables have a speed.Y >= 0f check for floor springs,
            // so we'll invert the gravity seen by the holdable so that the check passes.
            if (Orientation is CustomOrientations.Ceiling && (_version >= 4 ||
                                                              h.Entity is OriginalGliderState or TheoCrystalState)
                                                          && h is
                                                          {
                                                              SpeedGetter: { } getSpeed, SpeedSetter: { } setSpeed
                                                          }
                                                          // Inverted gravity holdables are weird, leave them alone (broken) right now.
                                                          && !(services.Gravity.IsActorInverted?.Invoke(
                                                              h.Entity as SourceActorEntityState) ?? false))
            {
                var speed = getSpeed();
                setSpeed(new Vector2(speed.X, -speed.Y));
                var ret = h.HitSpring(this);
                if (!ret)
                    setSpeed(speed);

                return ret;
            }

            return h.HitSpring(this);
        }

        private void NewOnHoldable(HoldableState h)
        {
            if (CallHitSpring(h))
            {
                AnimateSpringBounce();
                OnSuccessfulHit();

                // Apply speed multiplier
                if (h is { SpeedGetter: { }, SpeedSetter: { } })
                {
                    // Old implementation didn't work with custom holdables
                    if (_version < 2 && h.Entity is not OriginalGliderState and not TheoCrystalState)
                    {
                        return;
                    }

                    var speed = h.SpeedGetter();
                    switch (Orientation)
                    {
                        case CustomOrientations.Floor:
                            speed.Y *= SpeedMult.Y;
                            break;
                        case CustomOrientations.Ceiling:
                            speed.Y *= -SpeedMult.Y;
                            break;
                        default:
                            speed *= SpeedMult;
                            break;
                    }

                    h.SpeedSetter(speed);
                }
            }
        }

        private void NewOnPuffer(ISourcePufferEntity sourcePuffer)
        {
            var p = (OriginalPufferState)sourcePuffer;
            if (p.HitSpring(this))
            {
                p.HitSpeed *= SpeedMult;
                AnimateSpringBounce();
                OnSuccessfulHit();
            }
        }
    }
}
