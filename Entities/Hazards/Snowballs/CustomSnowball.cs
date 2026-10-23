using System;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Game.Entities.Hazards.Snowballs;
using Celeste.Game.Execution.Levels;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Physics.Collision;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Components.Oscillation;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation;
using Celeste.Simulation.Audio.Sources;
using Celeste.Simulation.Gameplay.Player.Collision.Components;

namespace Celeste.Mods.FrostHelper.Entities.Hazards.Snowballs
{
    [RuntimeTypeAlias("FrostHelper.CustomSnowball")]
    [SceneTracked(false)]
    public sealed class CustomSnowball : SceneEntityState
    {
        public enum AppearDirection
        {
            Right,
            Left,
            Top,
            Bottom
        }

        public float Speed;
        public float ResetTime;
        public bool DrawOutline;
        public AppearDirection appearDirection;
        public float SafeZoneSize;
        public float Offset;
        private bool leaving;
        public Sprite Sprite;
        private float resetTimer;
        private OriginalLevelSceneRoot level;
        public SineWaveState Sine;
        private float atY;
        private readonly SoundSourceComponentState spawnSfx;
        private readonly ColliderGeometry bounceCollider;
        private readonly IOriginalSnowballEnvironment engine;

        public CustomSnowball(ISourceEngineContext engine, string spritePath = "snowball", float speed = 200f,
            float resetTime = .8f, float sineWaveFrequency = .5f, bool drawOutline = true,
            AppearDirection dir = AppearDirection.Right, float safeZoneSize = 64f, float offset = 0f)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, Vector2.Zero)
        {
            this.engine = (IOriginalSnowballEnvironment)engine;
            appearDirection = dir;
            Speed = speed;
            if ((uint)(dir - 1) <= 1u) Speed = -speed;
            ResetTime = resetTime;
            DrawOutline = drawOutline;
            Depth = -12500;
            Collider = new HitboxColliderGeometry(12f, 9f, -5f, -2f);
            bounceCollider = new HitboxColliderGeometry(16f, 6f, -6f, -8f);
            Add(new PlayerColliderComponentState(OnPlayer));
            Add(new PlayerColliderComponentState(OnPlayerBounce, bounceCollider));
            Add(Sine = new SineWaveState(engine, sineWaveFrequency, 0f));
            CreateSprite(spritePath);
            Sprite.Play("spin");
            Add(spawnSfx = this.engine.SoundSources.Create());
            SafeZoneSize = safeZoneSize;
            Offset = offset;
        }

        public void StartLeaving() => leaving = true;

        public void CreateSprite(string path)
        {
            if (Sprite != null) Remove(Sprite);
            Add(Sprite = ((ISourceGameGraphicsEnvironment)engine).GameGraphics.SpriteFactory.Create(path));
            IsVertical();
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            level = Scene as OriginalLevelSceneRoot;
            ResetPosition();
        }

        private void ResetPosition()
        {
            var player =
                level.SourceScene.Entities.GetTrackedEntity<RoomPlayerSession>();
            if (player != null && CheckIfPlayerOutOfBounds(player))
            {
                spawnSfx.Play("event:/game/04_cliffside/snowball_spawn");
                Collidable = Visible = true;
                resetTimer = 0;
                if (IsVertical())
                {
                    Y = GetResetXPosition();
                    atY = X = player.CenterX + Offset;
                }
                else
                {
                    X = GetResetXPosition();
                    atY = Y = player.CenterY + Offset;
                }

                Sine.Reset();
                Sprite.Play("spin");
            }
            else resetTimer = .05f;
        }

        private bool IsVertical() => (uint)(appearDirection - 2) <= 1u;

        private bool CheckIfPlayerOutOfBounds(RoomPlayerSession player)
        {
            if (player == null) return false;
            return appearDirection switch
            {
                AppearDirection.Right => player.Right < level.Bounds.Right - SafeZoneSize,
                AppearDirection.Left => player.Left > level.Bounds.Left + SafeZoneSize,
                AppearDirection.Top => player.Top > level.Bounds.Top + SafeZoneSize,
                AppearDirection.Bottom => player.Bottom < level.Bounds.Bottom - SafeZoneSize,
                _ => throw new NotImplementedException($"Unknown direction for snowballs: {appearDirection}")
            };
        }

        private float GetResetXPosition() => appearDirection switch
        {
            AppearDirection.Right => level.Camera.Right + 10f,
            AppearDirection.Left => level.Camera.Left - 10f,
            AppearDirection.Top => level.Camera.Top - 10f,
            AppearDirection.Bottom => level.Camera.Bottom + 10f,
            _ => throw new NotImplementedException($"Unknown direction for snowballs: {appearDirection}")
        };

        private bool IsOutOfBounds() => appearDirection switch
        {
            AppearDirection.Right => X < level.Camera.Left - 60f,
            AppearDirection.Left => X > level.Camera.Right + 60f,
            AppearDirection.Top => Y > level.Camera.Bottom + 60f,
            AppearDirection.Bottom => Y < level.Camera.Top - 60f,
            _ => throw new NotImplementedException()
        };

        private void Destroy()
        {
            Collidable = false;
            Sprite.Play("break");
        }

        private void OnPlayer(RoomPlayerSession player)
        {
            player.Die(new Vector2(-1f, 0f));
            Destroy();
            engine.PlaySound("event:/game/04_cliffside/snowball_impact", Position);
        }

        private void OnPlayerBounce(RoomPlayerSession player)
        {
            if (!SourceEntityCollision.Check(this, player))
            {
                engine.Freeze(.1f);
                player.Bounce(Top - 2f);
                Destroy();
                engine.PlaySound("event:/game/general/thing_booped", Position);
            }
        }

        public override void Update()
        {
            base.Update();
            if (IsVertical())
            {
                Y -= Speed * engine.DeltaTime;
                X = atY + 4f * Sine.Value;
            }
            else
            {
                X -= Speed * engine.DeltaTime;
                Y = atY + 4f * Sine.Value;
            }

            if (!IsOutOfBounds()) return;
            if (leaving)
            {
                RemoveSelf();
                return;
            }

            resetTimer += engine.DeltaTime;
            if (resetTimer >= ResetTime) ResetPosition();
        }

        public override void Render(ISourceRenderContext context)
        {
            if (DrawOutline) Sprite.DrawOutline(context);
            base.Render(context);
        }
    }
}
