// SPDX-License-Identifier: MIT

using System;
using System.Collections;
using System.Linq;
using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Randomness;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest;
using Celeste.Everest.Diagnostics.Logging;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Modules.Runtime;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Mods.FrostHelper.Collider.Spinners;
using Celeste.Mods.FrostHelper.Modules;
using Celeste.Mods.FrostHelper.Runtime.Bubbles;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Components.Oscillation;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Player.Collision.Components;

namespace Celeste.Mods.FrostHelper.Entities.Bubbles
{
    [CustomEntity("FrostHelper/Bubbler")]
    [RuntimeTypeAlias("FrostHelper.Bubbler")]
    [SceneTracked(false)]
    public partial class Bubbler : SceneEntityState
    {
        private readonly ISourceEngineContext engine;
        private Vector2[] nodes;
        private Sprite sprite;
        private Sprite previewSprite;
        private SineWaveState sine;
        private bool InBubbler;

        public Bubbler(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
            this.engine = engine;
            var runtime = BubblerRuntime.For(engine);
            Collider = SpinnerHitboxReader.Create(data.Attr("hitbox"), 1f,
                           ((ISourceVectorEnvironment)engine).VectorArithmetic,
                           message => ((IEverestLoggerProcessEnvironment)engine).Logger.Log(EverestLogLevel.Error,
                               "FrostHelper", message))
                       ?? new HitboxColliderGeometry(14f, 14f);
            Collider.Center = Vector2.Zero;
            Add(new PlayerColliderComponentState(OnPlayer));
            nodes = data.NodesOffset(offset);
            if (data.Bool("visible"))
            {
                var random = ((ISourceRandomProcessEnvironment)engine).RandomProcess;
                random.PushRandom(runtime.VisualRandom);
                sine = new SineWaveState(engine, .6f);
                sine.Randomize();
                random.PopRandom();
                var module = ((ISourceEverestModulesEnvironment)engine).Modules.OfType<FrostHelperModule>().Single();
                var color = module.Colors.Read(data.Attr("color", "White"));
                var atlas = ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas;
                Add(sprite = new Sprite(engine, atlas, "objects/FrostHelper/bubble"));
                sprite.AddLoop("idle", "", .1f);
                sprite.CenterOrigin();
                sprite.Play("idle");
                sprite.SetColor(color);
                if (data.Bool("showReturnIndicator", true))
                {
                    previewSprite = new Sprite(engine, atlas, "objects/FrostHelper/bubble");
                    previewSprite.AddLoop("idle", "", .1f);
                    previewSprite.CenterOrigin();
                    previewSprite.Play("idle");
                    previewSprite.Position = nodes.Last() - new Vector2(0f, 10f);
                    previewSprite.Color = new PackedColor((int)color.R, (int)color.G, (int)color.B, 1) * .3f;
                }
            }
        }

        public override void Update()
        {
            if (sine != null)
            {
                sine.Update();
                float wobble = sine.Value;
                if (sprite != null) sprite.Y = wobble;
            }

            base.Update();
        }

        private void OnPlayer(RoomPlayerSession player)
        {
            Collidable = false;
            if (nodes != null && nodes.Length >= 2)
            {
                sprite?.RemoveSelf();
                Add(new SimulationCoroutine(engine, NodeRoutine(player)));
            }
        }

        private IEnumerator NodeRoutine(RoomPlayerSession player)
        {
            if (!player.Dead)
            {
                ((IOriginalGameplayProcess)engine).AudioSession.Play("event:/game/general/cassette_bubblereturn",
                    ((OriginalLevelSceneRoot)Scene).Camera.Position + new Vector2(160f, 90f),
                    ((IOriginalGameplayProcess)engine).SaveContext);
                player.Data.Dashes = Math.Max(player.Data.Dashes, player.Motor.BeforeState.MaxDashes);
                player.StartCassetteFly(nodes[1], nodes[0]);
                InBubbler = true;
            }

            yield break;
        }

        internal void FinishIfFlying()
        {
            if (InBubbler) OnBubbleFinish();
        }

        private void OnBubbleFinish()
        {
            InBubbler = false;
            previewSprite?.RemoveSelf();
            previewSprite = null;
        }
    }
}
