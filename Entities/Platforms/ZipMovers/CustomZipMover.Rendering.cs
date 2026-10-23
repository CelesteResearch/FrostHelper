using System.Runtime.CompilerServices;
using Celeste.Graphics.Display;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Celeste.Core.Scheduling;
using Celeste.Core.Components;
using Celeste.Core.Components.Timing;
using Celeste.Core.Display;
using Celeste.Core.Geometry;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Mathematics.Easing;
using Celeste.Core.Mathematics.LinearAlgebra;
using Celeste.Core.Rendering;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Modules.Runtime;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Formats.Map.Entities.Attributes;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Controls.Source;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Entities.Platforms.ZipMovers.Motion;
using Celeste.Game.Presentation.Platforms.ZipMovers;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Rendering;
using Celeste.Graphics.Rendering.Drawing;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation.Audio.Sources;
using Celeste.Simulation.Gameplay.Environment.Core;
using Celeste.Simulation.Gameplay.Player.Intro;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Platforms.Collision;
using Celeste.Simulation.Presentation.PostProcessing.Bloom;
using Celeste.Simulation.Presentation.Lighting;
using Celeste.Simulation.Presentation.Particles.Definitions;
using Celeste.Mods.FrostHelper.Modules;
using Celeste.Mods.FrostHelper.Runtime.Rendering;
using Celeste.Mods.FrostHelper.Runtime.ZipMovers;

namespace Celeste.Mods.FrostHelper.Entities.Platforms.ZipMovers
{
    public sealed partial class CustomZipMover
    {
        public override void Render(ISourceRenderContext context)
        {
            if (!FrostCameraCull.IsRectangleVisible(Position.X + Shake.X, Position.Y + Shake.Y, Width, Height, 4f,
                    ((OriginalLevelSceneRoot)Scene).Camera))
                return;

            bool rainbow = Rainbow;

            Vector2 position = Position;
            Position += Shake;
            if (_fillColor != PackedColor.Transparent)
            {
                var pixel = CurrentSource.Pixel();
                // Render the rect manually, to make use of our own pixel sprite,
                // which is packed to the same atlas as all the other sprites, saving a draw call.
                // We could set Draw.Pixel and call Draw.Rect as well, but this is slightly lower overhead and roughly same code size :shrug:
                ((ISourceDrawScope)context).Draw.SpriteBatch.Draw(pixel.Texture.Texture,
                    new IntRect((int)Position.X + 1, (int)Position.Y + 1, (int)Width - 2, (int)Height - 2),
                    pixel.ClipRect, _fillColor.PackedValue
                );
            }

            var visibleRect = Celeste.Mods.FrostHelper.Runtime.Lava.LavaCameraCull.GetVisibleSection(new(
                (int)Position.X, (int)Position.Y,
                (int)Width, (int)Height
            ), 4, ((OriginalLevelSceneRoot)Scene).Camera);
            var skipX = (visibleRect.X - (int)Position.X) / 8;
            var skipY = (visibleRect.Y - (int)Position.Y) / 8;
            var width = visibleRect.Right - Position.X;
            var height = visibleRect.Bottom - Position.Y;

            #region Cogs

            const float RotationPerX = 1.04719758f;
            int count = innerCogs.Count;
            int y = 4 + skipY * 8;
            while (y <= height)
            {
                int x = 4 + skipX * 8;

                while (x <= width)
                {
                    // Refactored math to make sure its consistent regardless of how many iterations we skip
                    var rotationOffset = ((x / 8) + (y / 8) * Width / 8) * RotationPerX;
                    int rotationDir = (y / 8 % 2 == 0) ? -1 : 1;
                    rotationDir *= (x / 8 % 2 == 0) ? -1 : 1;

                    int index = ZipMoverDrawing.CogFrameIndex(rotationOffset, rotationDir, percent, count);
                    AtlasEntry mtexture = innerCogs[index];
                    Vector2 offset = Vector2.Zero;
                    if (x <= 4 || x >= Width - 4f || y <= 4 || y >= Height - 4f)
                        mtexture = ZipMoverDrawing.ClipInnerCog(mtexture, mtexture.FrameWidth, mtexture.FrameHeight,
                            x <= 4,
                            x > 4 && x >= Width - 4f, y <= 4, y > 4 && y >= Height - 4f, temp, out offset);
                    Vector2 renderPos = Position + new Vector2(x, y) + offset;
                    PackedColor color = rainbow ? Module.Spinners.GetHue(Scene, renderPos) : tint;
                    SourceAtlasDrawing.DrawCentered(context, this, mtexture, renderPos,
                        color * (rotationDir < 0 ? .5f : 1f));

                    x += 8;
                }

                y += 8;
            }

            #endregion

            if (Rainbow)
            {
                streetlight.Color = Module.Spinners.GetHue(Scene, streetlight.RenderPosition);
            }

            #region Border

            {
                var widthBy8 = (int)width / 8 + (width % 8 > 0 ? 1 : 0);
                var heightBy8 = (int)height / 8 + (height % 8 > 0 ? 1 : 0);
                // Border textures only exist on the edges of the zipper
                // As such, we can avoid looping through the middle of the zipper at all

                // Handle left border:
                if (skipX == 0)
                {
                    DrawVerticalBorder(context, this, 0, skipY, heightBy8, 0f);
                }

                // Handle right border:
                if (widthBy8 == Width / 8f && widthBy8 > 1)
                {
                    DrawVerticalBorder(context, this, 2, skipY, heightBy8, Width - 8f);
                }

                // Handle top middle
                if (skipY == 0)
                {
                    DrawHorizontalBorder(context, this, 0, skipX, widthBy8, 0f);
                }

                // Handle bottom middle
                if (heightBy8 == Height / 8f && heightBy8 > 1)
                {
                    DrawHorizontalBorder(context, this, 2, skipX, widthBy8, Height - 8f);
                }

                // draws inner horizontal segments. Edges are already covered by DrawVerticalBorder
                static void DrawHorizontalBorder(ISourceRenderContext context, CustomZipMover mover, int edgeY,
                    int skipX,
                    int widthBy8, float yOffset)
                {
                    var pos = mover.Position;
                    if (skipX == 0)
                        skipX = 1;
                    if (widthBy8 == mover.Width / 8f)
                        widthBy8 -= 1;

                    pos.X += 8 * skipX;
                    pos.Y += yOffset;

                    int i = skipX;
                    while (i < widthBy8)
                    {
                        Draw(context, mover, 1, edgeY, pos);
                        pos.X += 8f;
                        i++;
                    }
                }

                static void DrawVerticalBorder(ISourceRenderContext context, CustomZipMover mover, int edgeX, int skipY,
                    int heightBy8, float xOffset)
                {
                    var pos = mover.Position;
                    pos.X += xOffset;
                    pos.Y += 8 * skipY;

                    int j = skipY;
                    while (j < heightBy8)
                    {
                        int edgeY = j == 0 ? 0 : j == mover.Height / 8f - 1f ? 2 : 1;
                        Draw(context, mover, edgeX, edgeY, pos);
                        j++;
                        pos.Y += 8f;
                    }
                }

                [MethodImpl(MethodImplOptions.AggressiveInlining)]
                static void Draw(ISourceRenderContext context, CustomZipMover mover, int edgeX, int edgeY,
                    Vector2 renderPos)
                {
                    var color = mover.Rainbow
                        ? mover.Module.Spinners.GetHue(mover.Engine.Frame.Scenes.CurrentScene, renderPos)
                        : mover.tint;

                    SourceAtlasDrawing.Draw(context, mover, mover.edges[edgeX, edgeY], renderPos, Vector2.Zero, color);
                }
            }

            #endregion

            base.Render(context);
            Position = position;
        }
    }
}
