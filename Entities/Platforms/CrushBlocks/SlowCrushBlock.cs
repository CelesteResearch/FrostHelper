using Celeste.Core.Display;
using System.Linq;
using Celeste.Everest.Modules.Runtime;
using Celeste.Mods.FrostHelper.Modules;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Simulation.Gameplay.Platforms.Collision;
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
    [Celeste.Everest.Modules.Entities.Metadata.CustomEntity("FrostHelper/SlowCrushBlock")]
    [Celeste.Core.Reflection.Runtime.RuntimeTypeAlias("FrostHelper.CustomCrushBlock")]
    public partial class SlowCrushBlock : SourceSolidEntityState,
        Celeste.Simulation.Gameplay.Platforms.Crushing.ICrushingSolidCollisionCallbackSource
    {
        public Action<Vector2, Vector2, ISourcePlatformEntity> CrushingCollisionCallback { get; set; }

        public enum Axes
        {
            Both,
            Horizontal,
            Vertical
        }

        private struct MoveState
        {
            public Vector2 From, Direction;

            public MoveState(Vector2 from, Vector2 direction)
            {
                From = from;
                Direction = direction;
            }
        }

        private ParticleTypePresentation P_Impact =>
            ((IOriginalGameplayProcess)engine).ParticleTypes.CrushBlock.P_Impact;

        private ParticleTypePresentation P_Crushing =>
            ((IOriginalGameplayProcess)engine).ParticleTypes.CrushBlock.P_Crushing;

        private ParticleTypePresentation P_Activate =>
            ((IOriginalGameplayProcess)engine).ParticleTypes.CrushBlock.P_Activate;

        public float CrushSpeed;

        public float CrushAccel;

        public float ReturnSpeed;

        public float ReturnAccel;

        private PackedColor fill = new PackedColor(98, 34, 43, 255);

        private OriginalLevelSceneRoot level;
        private readonly ISourceEngineContext engine;
        private readonly string spriteDirectory;

        private OriginalGameGraphicsResources Graphics =>
            (OriginalGameGraphicsResources)((ISourceGameGraphicsEnvironment)engine).GameGraphics;

        private SceneEntityRegistry Tracker => ((ISourceScene)Scene).SourceScene.Entities;

        private bool canActivate;

        private Vector2 crushDir;

        private List<MoveState> returnStack;

        private SimulationCoroutine attackCoroutine;

        private bool canMoveVertically;

        private bool canMoveHorizontally;

        private bool chillOut;

        private bool giant;

        private Sprite face;

        private string nextFaceDirection;

        private List<SpriteImage> idleImages = new List<SpriteImage>();

        private List<SpriteImage> activeTopImages = new List<SpriteImage>();

        private List<SpriteImage> activeRightImages = new List<SpriteImage>();

        private List<SpriteImage> activeLeftImages = new List<SpriteImage>();

        private List<SpriteImage> activeBottomImages = new List<SpriteImage>();

        private SoundSourceComponentState currentMoveLoopSfx;

        private SoundSourceComponentState returnLoopSfx;

        private bool Submerged => SourceSceneCollision.CheckRect(Tracker, "Celeste.Water",
            new IntRect((int)(base.Center.X - 4f), (int)base.Center.Y, 8, 4));

        public static SlowCrushBlock Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data)
            => new(((Celeste.Core.Scheduling.ISourceEngineContextScene)level).EngineContext, data, offset,
                ((ISourcePlatformCollisionScene)level).SolidWorldFactory.Create(data.Position + offset, false));

        public readonly string Directory;
        private readonly string sfxPrefix;

        private string Sound(string suffix) =>
            string.IsNullOrWhiteSpace(sfxPrefix) ? "event:/none" : sfxPrefix + "_" + suffix;

        private bool IsNoReturn => ReturnSpeed == 0f || ReturnAccel == 0f;

        public SlowCrushBlock(ISourceEngineContext engine, EntityData data, Vector2 offset, ISourceSolidWorld world)
            : base(data.Position + offset, data.Width, data.Height, false, world)
        {
            this.engine = engine;
            CrushAccel = data.Float("crushAcceleration", 250f);
            ReturnAccel = data.Float("returnAcceleration", 160f);
            ReturnSpeed = data.Float("returnSpeed", 60f);
            CrushSpeed = data.Float("crushSpeed", 120f);
            chillOut = data.Bool("chillout", false);
            Directory = data.Attr("directory", "objects/FrostHelper/slowcrushblock/");
            if (!Directory.EndsWith('/')) Directory += "/";
            spriteDirectory = Directory;
            sfxPrefix = data.Attr("sfxPrefix", "event:/game/06_reflection/crushblock");
            fill = ((ISourceEverestModulesEnvironment)engine).Modules.OfType<FrostHelperModule>().Single().Colors
                .Read(data.Attr("fillColor", "62222b"));
            Axes axes = EntityAttributeReader.Enum(data.Values, "axes", Axes.Both);
            OnDashCollide = OnDashed;
            returnStack = new List<MoveState>();
            giant = base.Width >= 48f && base.Height >= 48f && chillOut;
            canActivate = true;
            attackCoroutine = new SimulationCoroutine(engine);
            attackCoroutine.RemoveOnComplete = false;
            OwnedComponents.Add(attackCoroutine);
            List<AtlasEntry> atlasSubtextures =
                Graphics.GameAtlas.GetAtlasSubtextures(ResolveTexturePath("objects/crushblock/block"));
            AtlasEntry idle;
            switch (axes)
            {
                default:
                    idle = atlasSubtextures[3];
                    canMoveHorizontally = (canMoveVertically = true);
                    break;
                case Axes.Horizontal:
                    idle = atlasSubtextures[1];
                    canMoveHorizontally = true;
                    canMoveVertically = false;
                    break;
                case Axes.Vertical:
                    idle = atlasSubtextures[2];
                    canMoveHorizontally = false;
                    canMoveVertically = true;
                    break;
            }

            string faceId = giant ? "giant_crushblock_face" : "crushblock_face";
            face = data.Bool("reskinFace", false)
                ? Celeste.Mods.FrostHelper.Runtime.Rendering.FrostCustomSpriteFactory.Create(engine,
                    Graphics.SpriteBank, faceId, Directory)
                : Graphics.SpriteBank.Create(faceId);
            OwnedComponents.Add(face);
            face.Position =
                ((ISourceVectorEnvironment)engine).VectorArithmetic.Divide(new Vector2(base.Width, base.Height), 2f);
            face.Play("idle");
            face.OnLastFrame = delegate(string f)
            {
                if (f == "hit")
                {
                    face.Play(nextFaceDirection);
                }
            };
            int num = (int)(base.Width / 8f) - 1;
            int num2 = (int)(base.Height / 8f) - 1;
            AddImage(idle, 0, 0, 0, 0, -1, -1);
            AddImage(idle, num, 0, 3, 0, 1, -1);
            AddImage(idle, 0, num2, 0, 3, -1, 1);
            AddImage(idle, num, num2, 3, 3, 1, 1);
            for (int num3 = 1; num3 < num; num3++)
            {
                AddImage(idle, num3, 0, ((ISourceRandomEnvironment)engine).Random.Choose(1, 2), 0, 0, -1);
                AddImage(idle, num3, num2, ((ISourceRandomEnvironment)engine).Random.Choose(1, 2), 3, 0, 1);
            }

            for (int num4 = 1; num4 < num2; num4++)
            {
                AddImage(idle, 0, num4, 0, ((ISourceRandomEnvironment)engine).Random.Choose(1, 2), -1);
                AddImage(idle, num, num4, 3, ((ISourceRandomEnvironment)engine).Random.Choose(1, 2), 1);
            }

            OwnedComponents.Add(new LightOccludeState(0.2f));
            OwnedComponents.Add(returnLoopSfx = new SoundSourceComponentState(engine));
            OwnedComponents.Add(
                ((IWaterInteractionConstructionEnvironment)engine).WaterInteractions.Create(() =>
                    crushDir != Vector2.Zero));
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            level = ((OriginalLevelSceneRoot)Scene);
        }

        public override void Update()
        {
            base.Update();
            if (crushDir == Vector2.Zero)
            {
                face.Position =
                    ((ISourceVectorEnvironment)engine).VectorArithmetic.Divide(new Vector2(base.Width, base.Height),
                        2f);
                if (SourceTrackedEntityCollision.CheckAt(Tracker,
                        typeof(global::Celeste.Simulation.RoomPlayerSession).FullName, this,
                        SourceVector2Math.Add(Position, new Vector2(-1f, 0f))))
                {
                    face.Position.X -= 1f;
                }
                else if (SourceTrackedEntityCollision.CheckAt(Tracker,
                             typeof(global::Celeste.Simulation.RoomPlayerSession).FullName, this,
                             SourceVector2Math.Add(Position, new Vector2(1f, 0f))))
                {
                    face.Position.X += 1f;
                }
                else if (SourceTrackedEntityCollision.CheckAt(Tracker,
                             typeof(global::Celeste.Simulation.RoomPlayerSession).FullName, this,
                             SourceVector2Math.Add(Position, new Vector2(0f, -1f))))
                {
                    face.Position.Y -= 1f;
                }
            }

            if (currentMoveLoopSfx != null)
            {
                currentMoveLoopSfx.Param("submerged", Submerged ? 1 : 0);
            }

            if (returnLoopSfx != null)
            {
                returnLoopSfx.Param("submerged", Submerged ? 1 : 0);
            }
        }

        public override void Render(ISourceRenderContext context)
        {
            Vector2 position = Position;
            Position = SourceVector2Math.Add(Position, base.Shake);
            SourcePrimitiveDrawing.Rect(context, this, base.X + 2f, base.Y + 2f, base.Width - 4f, base.Height - 4f,
                fill);
            base.Render(context);
            Position = position;
        }

        private void AddImage(AtlasEntry idle, int x, int y, int tx, int ty, int borderX = 0, int borderY = 0)
        {
            AtlasEntry subtexture = idle.GetSubtexture(tx * 8, ty * 8, 8, 8);
            Vector2 vector = new Vector2(x * 8, y * 8);
            if (borderX != 0)
            {
                SpriteImage image = new SpriteImage(subtexture);
                image.Color = PackedColor.Black;
                image.Position = SourceVector2Math.Add(vector, new Vector2(borderX, 0f));
                OwnedComponents.Add(image);
            }

            if (borderY != 0)
            {
                SpriteImage image2 = new SpriteImage(subtexture);
                image2.Color = PackedColor.Black;
                image2.Position = SourceVector2Math.Add(vector, new Vector2(0f, borderY));
                OwnedComponents.Add(image2);
            }

            SpriteImage image3 = new SpriteImage(subtexture);
            image3.Position = vector;
            OwnedComponents.Add(image3);
            idleImages.Add(image3);
            if (borderX != 0 || borderY != 0)
            {
                if (borderX < 0)
                {
                    SpriteImage image4 = new SpriteImage(Graphics
                        .GameAtlas[ResolveTexturePath("objects/crushblock/lit_left")]
                        .GetSubtexture(0, ty * 8, 8, 8));
                    activeLeftImages.Add(image4);
                    image4.Position = vector;
                    image4.Visible = false;
                    OwnedComponents.Add(image4);
                }
                else if (borderX > 0)
                {
                    SpriteImage image5 = new SpriteImage(Graphics
                        .GameAtlas[ResolveTexturePath("objects/crushblock/lit_right")]
                        .GetSubtexture(0, ty * 8, 8, 8));
                    activeRightImages.Add(image5);
                    image5.Position = vector;
                    image5.Visible = false;
                    OwnedComponents.Add(image5);
                }

                if (borderY < 0)
                {
                    SpriteImage image6 =
                        new SpriteImage(Graphics.GameAtlas[ResolveTexturePath("objects/crushblock/lit_top")]
                            .GetSubtexture(tx * 8, 0, 8, 8));
                    activeTopImages.Add(image6);
                    image6.Position = vector;
                    image6.Visible = false;
                    OwnedComponents.Add(image6);
                }
                else if (borderY > 0)
                {
                    SpriteImage image7 = new SpriteImage(Graphics
                        .GameAtlas[ResolveTexturePath("objects/crushblock/lit_bottom")]
                        .GetSubtexture(tx * 8, 0, 8, 8));
                    activeBottomImages.Add(image7);
                    image7.Position = vector;
                    image7.Visible = false;
                    OwnedComponents.Add(image7);
                }
            }
        }

        protected SoundSourceComponentState CurrentMoveLoopSource => currentMoveLoopSfx;
        protected SoundSourceComponentState ReturnMoveLoopSource => returnLoopSfx;
        protected Sprite SourceFace => face;
        protected bool SourceGiant => giant;

        protected PackedColor SourceFill
        {
            get => fill;
            set => fill = value;
        }

        private string ResolveTexturePath(string path)
            => spriteDirectory == null ? path : Directory + path.Substring("objects/crushblock/".Length);

        private string ResolveSoundPath(string path) =>
            Sound(path.Substring("event:/game/06_reflection/crushblock_".Length));

        protected Vector2 SourceCrushDirection => crushDir;
        protected virtual void StopMoveLoopForSlowdown() => currentMoveLoopSfx.Stop();

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private bool MoveHCheck(float amount) =>
            Celeste.Simulation.Gameplay.Platforms.Crushing.CrushingSolidMotion.CheckHorizontal(this, level, amount,
                CrushingCollisionCallback);

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private bool MoveVCheck(float amount) =>
            Celeste.Simulation.Gameplay.Platforms.Crushing.CrushingSolidMotion
                .CheckVertical(this, level, amount, true, CrushingCollisionCallback);
    }
}
