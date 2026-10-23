// SPDX-License-Identifier: MIT

using System;
using System.Numerics;
using Celeste.Core.Audio;
using Celeste.Core.Mathematics;
using Celeste.Core.Randomness;
using Celeste.Core.Rendering;
using Celeste.Graphics.Display;
using Celeste.Mods.FrostHelper.Content.Spinners;
using Celeste.Mods.FrostHelper.Entities.Debris;
using Celeste.Mods.FrostHelper.Spinners.Rendering;
using Celeste.Scene.Tracking;
using Celeste.Scene.Entities;
using Celeste.Simulation.Gameplay.Collision;
using Celeste.Simulation.Gameplay.Environment.Core;
using Celeste.Simulation.Gameplay.Platforms.Foundation;

namespace Celeste.Mods.FrostHelper.Entities.Spinners
{
    public partial class CustomSpinner
    {
        private bool UseHotSprites => isCore && Level.CoreMode == RoomCoreMode.Hot;

        protected override void CreateSprites()
        {
            if (expanded) return;
            Unregister();
            group.Register(this);
            var process = ((ISourceRandomProcessEnvironment)engine).RandomProcess;
            process.PushRandom(randomSeed);
            var foreground = SpriteSource.GetFgTextures(UseHotSprites);
            var texture = Random.Choose(foreground);
            foreach (object entity in ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntities(
                         typeof(CustomSpinner)))
            {
                var other = (CustomSpinner)entity;
                float imageScale = ImageScale + other.ImageScale;
                if (other.ID > ID && other.AttachGroup == AttachGroup && other.AttachToSolid == AttachToSolid &&
                    VectorArithmetic.LengthSquared(other.Position - Position) <
                    SpriteSource.ConnectionWidth * other.SpriteSource.ConnectionWidth * MathF.Pow(imageScale, 2f) / 4f)
                    AddSprite((Position + other.Position) / 2f - Position);
            }

            bool topLeft = SingleFGImage || !SpinnerSolidCheck(Position + new Vector2(-4f, -4f));
            bool topRight = SingleFGImage || !SpinnerSolidCheck(Position + new Vector2(4f, -4f));
            bool bottomLeft = SingleFGImage || !SpinnerSolidCheck(Position + new Vector2(4f, 4f));
            bool bottomRight = SingleFGImage || !SpinnerSolidCheck(Position + new Vector2(-4f, 4f));
            if (SingleFGImage || topLeft && topRight && bottomLeft && bottomRight)
                AddForeground(texture, texture.First.Center, texture.CreateRandomAnimOffset(Random));
            else AddCorners(texture, topLeft, topRight, bottomLeft, bottomRight);
            if (SpriteSource.HasDeco)
            {
                var decoTexture = SpriteSource.GetDecoTextures(UseHotSprites)[foreground.IndexOf(texture)];
                AddDecoration(CreateImage(decoTexture, decoTexture.First.Center, 0f), ImageScale);
            }

            expanded = true;
            process.PopRandom();
        }

        private SpinnerRenderImage CreateImage(SpinnerTextureSequence texture, Vector2 origin, float animationOffset)
            => new(time => texture.GetFrame(time, animationOffset)) { Origin = origin, Color = Tint };

        private void AddForeground(SpinnerTextureSequence texture, Vector2 origin, float animationOffset)
            => sprites.Add(CreateImage(texture, origin, animationOffset));

        private void AddCorners(SpinnerTextureSequence texture, bool topLeft, bool topRight, bool bottomLeft,
            bool bottomRight)
        {
            int halfX = texture.First.FrameWidth / 2, halfY = texture.First.FrameHeight / 2;
            float animationOffset = texture.CreateRandomAnimOffset(Random);
            if (topLeft && topRight) Add(0, 0, texture.First.FrameWidth + 4, halfY + 2, halfX, halfY);
            else
            {
                if (topLeft) Add(0, 0, halfX + 2, halfY + 2, halfX, halfY);
                if (topRight) Add(halfX - 2, 0, halfX + 2, halfY + 2, 2f, halfY);
            }

            if (bottomLeft && bottomRight) Add(0, halfY - 2, texture.First.FrameWidth + 4, halfY + 2, halfX, 2f);
            else
            {
                if (bottomLeft) Add(halfX - 2, halfY - 2, halfX + 2, halfY + 2, 2f, 2f);
                if (bottomRight) Add(0, halfY - 2, halfX + 2, halfY + 2, halfX, 2f);
            }

            void Add(int x, int y, int width, int height, float ox, float oy)
            {
                var subtexture = texture.Animated
                    ? texture.GetSubtextureCached(x, y, width, height)
                    : texture.GetSubtexture(x, y, width, height);
                AddForeground(subtexture, new Vector2(ox, oy), animationOffset);
            }
        }

        public void AddSprite(Vector2 at)
        {
            at = new Vector2((float)Math.Floor(at.X), (float)Math.Floor(at.Y));
            var background = SpriteSource.GetBgTextures(UseHotSprites);
            var color = Rainbow ? GetHue(Position + at) : Tint;
            var texture = Random.Choose(background);
            float rotation = Random.Choose(0, 1, 2, 3) * 1.57079637f;
            var image = CreateImage(texture, texture.First.Center, texture.CreateRandomAnimOffset(Random));
            image.Color = color;
            // The original calls ToRad on an already radian rotation; preserve that exact pixel alignment.
            var origin = VectorArithmetic.Rotate(texture.First.Center, rotation * ((float)Math.PI / 180f));
            image.Position = Round(at) + origin - Round(origin);
            image.Rotation = rotation;
            fills.Add(image);
            if (SpriteSource.HasDeco)
            {
                var decoTexture = SpriteSource.GetBgDecoTextures(UseHotSprites)[background.IndexOf(texture)];
                var decoration = CreateImage(decoTexture, decoTexture.First.Center, 0f);
                decoration.Position = at;
                decoration.Rotation = rotation;
                AddDecoration(decoration, 1f);
            }
        }

        private static Vector2 Round(Vector2 at) => new((float)Math.Round(at.X), (float)Math.Round(at.Y));

        private bool SpinnerSolidCheck(Vector2 at)
        {
            if (AttachToSolid || MoveWithWind) return false;
            foreach (var solid in SourceSceneCollision.AllPoint<SourceSolidEntityState>(
                         ((ISourceScene)Scene).SourceScene.Entities, "Celeste.Solid", at))
                if (solid is ISourceSolidTilesEntity && solid.Depth <= Depth)
                    return true;
            return false;
        }

        public override void Render(ISourceRenderContext context)
        {
            if (Controller.CanUseBlackOutlineRenderTargetOpt && Controller.CanUseRenderTargetRender) return;
            base.Render(context);
            group.Border.RenderForeground(this);
        }

        public void RenderDecoration(ISourceRenderContext context)
        {
            foreach (var decoration in decorations)
            {
                var image = decoration.Image;
                // Source decoration entities do not inherit the foreground's accumulated shake.
                var texture = image.TextureAt(0f);
                Celeste.Graphics.Rendering.SourceAtlasDrawing.Draw(context, this, texture,
                    decorationPosition + image.Position, image.Origin, image.Color, Vector2.One * decoration.Scale,
                    image.Rotation);
            }
        }

        private void AddDecoration(SpinnerRenderImage image, float imageScale)
        {
            if (!decorationCreated)
            {
                decorationCreated = true;
                decorationPosition = Position;
            }

            decorations.Add((image, imageScale));
        }

        public override void Destroy(bool boss = false)
        {
            if (InView())
            {
                ((ISourceSpatialAudioEnvironment)engine).SpatialAudio.Play("event:/game/06_reflection/fall_spike_smash",
                    Position);
                var color = Rainbow
                    ? GetHue(Position)
                    : Celeste.Graphics.Display.Colors.SourceHexColor.HexToColor(destroyColor);
                FastCrystalDebris.Burst(engine, Position, color, boss, DestroyDebrisCount);
            }

            RemoveSelf();
        }
    }
}
