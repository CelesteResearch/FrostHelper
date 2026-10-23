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
    public partial class CustomSpring
    {
        public override void Update()
        {
            base.Update();

            _inactiveTimer -= SpringProcess.DeltaTime;
        }

        public override void Render(ISourceRenderContext renderContext)
        {
            if (!IsSpriteVisible()) return;

            if (Collidable && _renderOutline)
                DrawSpringOutline(renderContext);
            Sprite.Render(renderContext);
        }

        private void DrawSpringOutline(ISourceRenderContext context)
        {
            var position = Sprite.RenderPosition;
            DrawOutlineAt(context, position - Vector2.UnitY);
            DrawOutlineAt(context, position + Vector2.UnitY);
            DrawOutlineAt(context, position - Vector2.UnitX);
            DrawOutlineAt(context, position + Vector2.UnitX);
            DrawOutlineAt(context, position - Vector2.One);
            DrawOutlineAt(context, position + Vector2.One);
            DrawOutlineAt(context, position + new Vector2(-1f, 1f));
            DrawOutlineAt(context, position + new Vector2(1f, -1f));
        }

        private void DrawOutlineAt(ISourceRenderContext context, Vector2 position)
            => Celeste.Graphics.Rendering.SourceAtlasDrawing.Draw(context, this, Sprite.Texture, position,
                Sprite.Origin,
                PackedColor.Black, Sprite.Scale, Sprite.Rotation);

        private bool IsSpriteVisible()
        {
            var camera = ((OriginalLevelSceneRoot)Scene).Camera;
            var x = (int)Sprite.RenderPosition.X - (int)Sprite.Origin.X / 2 - camera.Left;
            var y = (int)Sprite.RenderPosition.Y - (int)Sprite.Origin.Y / 2 - camera.Top;
            var width = (int)(Sprite.Width * Sprite.Scale.X);
            var height = (int)(Sprite.Height * Sprite.Scale.Y);
            return x + width >= -4f && y + height >= -4f && x <= 4f + camera.Viewport.Width * camera.ZoomVector.X &&
                   y <= 4f + camera.Viewport.Height * camera.ZoomVector.Y;
        }

        // (Also called at the beginning of BounceAnimate via hook)
        private void OnSuccessfulHit()
        {
            if (_oneUse && Collidable)
            {
                Collidable = false;
                OwnedComponents.Add(new SimulationCoroutine(SpringProcess, BreakRoutine()));
            }
        }

        private void OnSuccessfulPlayerHit(RoomPlayerSession player, SpringRecovery.SavedPlayerData prev)
        {
            OnSuccessfulHit();
            _recovery.Recover(player, prev);
        }

        public IEnumerator BreakRoutine()
        {
            Collidable = false;
            ((IOriginalGameInputProcessEnvironment)SpringProcess).InputSource.Rumble(RumbleStrength.Medium,
                RumbleLength.Medium);
            SpringProcess.AudioSession.Play("event:/game/general/platform_disintegrate", Center,
                SpringProcess.SaveContext);
            foreach (SpriteImage image in OwnedComponents.GetAll<SpriteImage>())
            {
                ((OriginalLevelSceneRoot)Scene).Particles.Emit(SpringProcess.ParticleTypes.CrumblePlatform.P_Crumble, 2,
                    Position + image.Position, Vector2.One * 3f);
            }

            float t = 1f;
            while (t > 0f)
            {
                foreach (SpriteImage image in OwnedComponents.GetAll<SpriteImage>())
                {
                    image.Scale = Vector2.One * t;
                    image.Rotation += SpringProcess.DeltaTime * 4;
                }

                t -= SpringProcess.DeltaTime * 4;
                yield return null;
            }

            Visible = false;
            RemoveSelf();
        }

        private static readonly ParticleTypePresentation P_Crumble_Up = new()
        {
            Color = new PackedColor(132, 126, 135),
            FadeMode = ParticleTypePresentation.FadeModes.Late,
            Size = 1f,
            Direction = ((float)Math.PI / 2f),
            SpeedMin = -5f,
            SpeedMax = -25f,
            LifeMin = 0.8f,
            LifeMax = 1f,
            Acceleration = Vector2.UnitY * -20f
        };

        private static readonly ParticleTypePresentation P_Crumble_Down = new()
        {
            Color = new PackedColor(132, 126, 135),
            FadeMode = ParticleTypePresentation.FadeModes.Late,
            Size = 1f,
            Direction = ((float)Math.PI / 2f),
            SpeedMin = 5f,
            SpeedMax = 25f,
            LifeMin = 0.8f,
            LifeMax = 1f,
            Acceleration = Vector2.UnitY * 20f
        };

        private static readonly ParticleTypePresentation P_Crumble_Left = new()
        {
            Color = new PackedColor(132, 126, 135),
            FadeMode = ParticleTypePresentation.FadeModes.Late,
            Size = 1f,
            Direction = 0f,
            SpeedMin = 5f,
            SpeedMax = 25f,
            LifeMin = 0.8f,
            LifeMax = 1f,
            Acceleration = Vector2.UnitY * 20f
        };

        private static readonly ParticleTypePresentation P_Crumble_Right = new()
        {
            Color = new PackedColor(132, 126, 135),
            FadeMode = ParticleTypePresentation.FadeModes.Late,
            Size = 1f,
            Direction = 0f,
            SpeedMin = -5f,
            SpeedMax = -25f,
            LifeMin = 0.8f,
            LifeMax = 1f,
            Acceleration = Vector2.UnitY * -20f
        };

        private IEnumerator OneUseParticleRoutine()
        {
            while (true)
            {
                switch (Orientation)
                {
                    case CustomOrientations.Floor:
                        ((OriginalLevelSceneRoot)Scene).Particles.Emit(P_Crumble_Up, 2, Position, new(3f));
                        break;
                    case CustomOrientations.WallRight:
                        ((OriginalLevelSceneRoot)Scene).Particles.Emit(P_Crumble_Right, 2, Position, new(2f));
                        break;
                    case CustomOrientations.WallLeft:
                        ((OriginalLevelSceneRoot)Scene).Particles.Emit(P_Crumble_Left, 2, Position, new(2f));
                        break;
                    case CustomOrientations.Ceiling:
                        ((OriginalLevelSceneRoot)Scene).Particles.Emit(P_Crumble_Down, 2, Position, new(2f));
                        break;
                }

                yield return 0.25f;
            }
        }
    }
}
