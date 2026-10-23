// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Maps;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Modules.Runtime;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Gameplay.Environment.Water;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Graphics.Effects.Outlines;
using Celeste.Game.Presentation.Rendering;
using Celeste.Mods.FrostHelper.Runtime.Lighting;
using Celeste.Mods.FrostHelper.Modules;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Entities;
using Celeste.Simulation;
using Celeste.Simulation.Audio.Sources;
using Celeste.Simulation.Gameplay.Environment.HangingLamps;
using Celeste.Simulation.Presentation.Lighting;
using Celeste.Simulation.Presentation.PostProcessing.Bloom;

namespace Celeste.Mods.FrostHelper.Entities.Lighting
{
    [CustomEntity("FrostHelper/ColoredHangingLamp")]
    [Celeste.Core.Reflection.Runtime.RuntimeTypeAlias("FrostHelper.ColoredHangingLamp")]
    public class ColoredHangingLamp : SceneEntityState, ISourceEngineContextEntity
    {
        private readonly ISourceEngineContext engine;
        private readonly ColoredLampOutlines.Holder outlines;
        private readonly List<SpriteImage> images = new();
        private readonly BloomPointState bloom;
        private readonly VertexLightState light;
        private readonly SoundSourceComponentState sound;
        private readonly string soundPath;
        private readonly string spritePath;
        private readonly PackedColor outlineColor;
        private readonly Action playSwingSound;
        private readonly Action beginGameplay;
        private ISourceRenderContext renderContext;
        private float speed;
        private float rotation;
        private float soundDelay;
        public readonly int Length;
        ISourceEngineContext ISourceEngineContextEntity.EngineContext => engine;

        public static ColoredHangingLamp Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) => new(data, offset, level.EngineContext);

        public ColoredHangingLamp(EntityData data, Vector2 offset, ISourceEngineContext engine)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration)
        {
            this.engine = engine;
            outlines = ColoredLampOutlines.Bind(engine);
            var colors = ((ISourceEverestModulesEnvironment)engine).Modules.OfType<FrostHelperModule>().Single().Colors;
            soundPath = data.Attr("sfx", "event:/game/02_old_site/lantern_hit");
            outlineColor = colors.Read(data.Attr("spriteOutlineColor", "000000"));
            Position = offset + data.Position + Vector2.UnitX * 4f;
            Length = Math.Max(8, data.Height);
            Depth = 2000;
            spritePath = data.Attr("sprite", "objects/hanginglamp");
            var texture = ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas[spritePath];
            var color = colors.Read(data.Attr("spriteColor", "ffffff"));
            var middle = texture.GetSubtexture(0, 8, 8, 8);
            for (int i = 0; i < Length - 8; i += 8)
                AddImage(new SpriteImage(middle) { Origin = new Vector2(4f, -i), Color = color });
            AddImage(new SpriteImage(texture.GetSubtexture(0, 16, 8, 8))
                { Origin = new Vector2(4f, -(Length - 8)), Color = color });
            AddImage(new SpriteImage(texture.GetSubtexture(0, 0, 8, 8))
                { Origin = new Vector2(4f, 0f), Color = color });
            Add(bloom = new BloomPointState(Vector2.UnitY * (Length - 4), data.Float("bloomAlpha", 1f),
                data.Float("bloomRadius", 48f)));
            Add(light = new VertexLightState(engine, Vector2.UnitY * (Length - 4),
                colors.Read(data.Attr("color", "ffffff")),
                data.Float("alpha", 1f), data.Int("startFade", 24), data.Int("endFade", 48)));
            Add(sound = ((IOriginalWaterfallSoundEnvironment)engine).SoundSources.Create());
            playSwingSound = PlaySwingSound;
            beginGameplay = BeginGameplay;
            Collider = new HitboxColliderGeometry(8f, Length, -4f, 0f);
        }

        private void AddImage(SpriteImage image)
        {
            image.SetRawEntity(this);
            image.Texture.AtlasPath = spritePath;
            images.Add(image);
        }

        public override void Update()
        {
            base.Update();
            soundDelay -= engine.DeltaTime;
            var player =
                ((OriginalLevelSceneRoot)Scene).Scheduler.Entities.GetTrackedEntity<RoomPlayerSession>(
                    typeof(RoomPlayerSession).FullName);
            bool touching = player != null && Collider.Collide(player.Collider);
            if (!HangingLampPhysics.AdvanceWhenMoving(ref speed, ref rotation, ref soundDelay, engine, touching,
                    touching ? player.Data.Motion.Normal.Speed.X : 0f, touching ? player.Y - Y : 0f,
                    Length, null, ((ISourceExtremaEnvironment)engine).ExtremaArithmetic, playSwingSound)) return;
            for (int i = 0; i < images.Count - 1; i++) images[i].Rotation = rotation;
            Vector2 position = Vector2Geometry.AngleToVector(rotation + (float)Math.PI / 2f, Length - 4f);
            bloom.Position = light.Position = sound.Position = position;
        }

        private void PlaySwingSound() => sound.Play(soundPath);

        private void BeginGameplay() => GameplayRendererState.Begin(renderContext);

        public override void Render(ISourceRenderContext context)
        {
            Vector2 endpoint = Position + Vector2Geometry.AngleToVector(rotation + (float)Math.PI / 2f, Length);
            int left = Math.Min((int)Position.X, (int)endpoint.X);
            int top = Math.Min((int)Position.Y, (int)endpoint.Y);
            int right = Math.Max((int)Position.X, (int)endpoint.X);
            int bottom = Math.Max((int)Position.Y, (int)endpoint.Y);
            var camera = ((OriginalLevelSceneRoot)Scene).Camera;
            if (right - camera.Left < -8f || bottom - camera.Top < -8f ||
                left - camera.Left > 8f + camera.Viewport.Width * camera.ZoomVector.X ||
                top - camera.Top > 8f + camera.Viewport.Height * camera.ZoomVector.Y) return;
            if (outlineColor != default)
            {
                if (rotation == 0f)
                {
                    renderContext = context;
                    var outline = outlines.Value;
                    foreach (var image in images) outline.Render(context, image, outlineColor, true, beginGameplay);
                }
                else
                {
                    foreach (var image in images) CachedImageOutline.RenderFast(context, image, outlineColor);
                }
            }

            foreach (var image in images) image.Render(context);
            // Rendering deliberately skips Entity.Render hooks, as in the released module.
        }
    }
}
