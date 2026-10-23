using System;
using System.Numerics;
using Celeste.Core.Audio;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Geometry;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Framework;
using Celeste.Core.Mathematics.LinearAlgebra;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Scheduling;
using Celeste.Core.Randomness;
using Celeste.Core.Rendering;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Entities.Platforms.SwapBlocks.Motion;
using Celeste.Game.Entities.Platforms.SwapBlocks.Rendering;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Geometry.TwoD.Camera;
using Celeste.Graphics.Rendering;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Platforms.Collision;
using Celeste.Simulation.Gameplay.Player.Dash;
using Celeste.Simulation.Presentation.Particles.Definitions;
using Celeste.Simulation.Presentation.Lighting;
using Celeste.Simulation.Presentation.PostProcessing.Displacement;

namespace Celeste.Mods.FrostHelper.Entities.Platforms.SwapBlocks
{
    [CustomEntity("FrostHelper/ToggleSwapBlock")]
    [SceneTracked(false)]
    [RuntimeTypeAlias("FrostHelper.ToggleSwapBlock")]
    public partial class ToggleSwapBlock : SourceSolidEntityState
    {
        internal ISourceEngineContext Engine => SourceWorld.EngineContext;
        internal AtlasData GameAtlas => ((ISourceGameGraphicsEnvironment)Engine).GameGraphics.GameAtlas;
        private ISourceSpatialAudio Audio => ((ISourceSpatialAudioEnvironment)Engine).SpatialAudio;
        private ISourceScalarArithmetic Scalar => ((ISourceScalarEnvironment)Engine).ScalarArithmetic;
        private ISourceExtremaArithmetic Extrema => ((ISourceExtremaEnvironment)Engine).ExtremaArithmetic;
        private ISourceVectorArithmetic Vectors => ((ISourceVectorEnvironment)Engine).VectorArithmetic;
        private bool OnInterval(float interval) => ((ISourceScene)Scene).SourceScene.Clock.OnInterval(interval, Engine);

        internal bool IsVisible(float x, float y, float width, float height)
        {
            var camera = ((OriginalLevelSceneRoot)Scene).Camera;
            return Celeste.Mods.FrostHelper.Runtime.Rendering.FrostCameraCull.IsRectangleVisible(x, y, width, height,
                4f,
                camera);
        }

        public static ToggleSwapBlock Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room, Vector2 offset,
            OriginalLevelEntityData data)
            => new(data, offset,
                ((ISourcePlatformCollisionScene)level).SolidWorldFactory.Create(data.Position + offset, true));

        internal string directory = "objects/swapblock";
        internal bool renderBG = false;

        public string MoveSFX, MoveEndSFX;
        public bool EmitParticles;
        public PackedColor? ParticleColor;

        public ToggleSwapBlock(EntityData data, Vector2 offset, ISourceSolidWorld world) : base(data.Position + offset,
            data.Width, data.Height, true, world)
        {
            MoveSFX = data.Attr("moveSFX", "event:/game/05_mirror_temple/swapblock_move");
            MoveEndSFX = data.Attr("moveEndSFX", "event:/game/05_mirror_temple/swapblock_move_end");

            directory = data.Attr("directory", "objects/swapblock");
            if (directory == "objects/swapblock")
            {
                directory = data.Attr("sprite", "objects/swapblock");
            }

            renderBG = data.Bool("renderBG", false);
            Vector2 node = data.Nodes[0] + offset;
            redAlpha = 1f;
            start = Position;
            end = node;
            maxForwardSpeed = data.Float("speed", 360f) / Vectors.Distance(start, end);
            maxBackwardSpeed = maxForwardSpeed * 0.4f;
            Direction.X = Math.Sign(end.X - start.X);
            Direction.Y = Math.Sign(end.Y - start.Y);
            OwnedComponents.Add(new DashListenerState
            {
                OnDash = OnDash
            });
            int num = (int)SourceFrameworkMath.Min(X, node.X);
            int num2 = (int)SourceFrameworkMath.Min(Y, node.Y);
            int num3 = (int)SourceFrameworkMath.Max(X + Width, node.X + Width);
            int num4 = (int)SourceFrameworkMath.Max(Y + Height, node.Y + Height);
            moveRect = new IntRect(num, num2, num3 - num, num4 - num2);
            AtlasEntry mtexture = GameAtlas[directory + "/block"];
            AtlasEntry mtexture2 = GameAtlas[directory + "/blockRed"];
            AtlasEntry mtexture3 = GameAtlas[directory + "/target"];
            nineSliceGreen = new AtlasEntry[3, 3];
            nineSliceRed = new AtlasEntry[3, 3];
            nineSliceTarget = new AtlasEntry[3, 3];
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    nineSliceGreen[i, j] = mtexture.GetSubtexture(i * 8, j * 8, 8, 8);
                    nineSliceRed[i, j] = mtexture2.GetSubtexture(i * 8, j * 8, 8, 8);
                    nineSliceTarget[i, j] = mtexture3.GetSubtexture(i * 8, j * 8, 8, 8);
                }
            }

            middleGreen = new Sprite(Engine, GameAtlas, directory + "/midBlock");
            middleGreen.AddLoop("idle", "", 0.08f);
            middleGreen.Justify = new Vector2(0.5f, 0.5f);
            middleGreen.Play("idle");
            OwnedComponents.Add(middleGreen);
            middleRed = new Sprite(Engine, GameAtlas, directory + "/midBlockRed");
            middleRed.AddLoop("idle", "", 0.08f);
            middleRed.Justify = new Vector2(0.5f, 0.5f);
            middleRed.Play("idle");

            OwnedComponents.Add(middleRed);
            OwnedComponents.Add(new LightOccludeState(0.2f));
            Depth = -9999;

            ParticleType =
                new ParticleTypePresentation(((IOriginalGameplayProcess)Engine).ParticleTypes.SwapBlock.P_Move)
                {
                    Color = PackedColor.FromHex(data.Attr("particleColor1", "fbf236")),
                    Color2 = PackedColor.FromHex(data.Attr("particleColor2", "6abe30"))
                };

            EmitParticles = data.Bool("emitParticles", true);
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            ((ISourceScene)scene).SourceScene.QueueSourceEntity(path = new ToggleSwapBlockPath(this));
        }

        public override void Removed(object scene)
        {
            base.Removed(scene);
            Audio.Stop(moveSfx, true);
            Audio.Stop(returnSfx, true);
        }

        public override void SceneEnd(object scene)
        {
            base.SceneEnd(scene);
            Audio.Stop(moveSfx, true);
            Audio.Stop(returnSfx, true);
        }

        public ParticleTypePresentation ParticleType;

        private const float ReturnTime = 0.8f;

        public Vector2 Direction;

        public bool Swapping;

        internal Vector2 start;

        internal Vector2 end;

        private float lerp;

        private int target;

        internal IntRect moveRect;

        private float speed;

        private float maxForwardSpeed;

        private float maxBackwardSpeed;

        private float returnTimer;

        private float redAlpha;

        private AtlasEntry[,] nineSliceGreen;

        private AtlasEntry[,] nineSliceRed;

        internal AtlasEntry[,] nineSliceTarget;

        private Sprite middleGreen;

        private Sprite middleRed;

        private ToggleSwapBlockPath path;

        private ISourceAudioEventReference moveSfx;

#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
        private ISourceAudioEventReference returnSfx;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value

        private DisplacementBurst burst;

        private float particlesRemainder;
    }
}
