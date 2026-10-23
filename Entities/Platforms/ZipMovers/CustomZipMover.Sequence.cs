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
        private IEnumerator Sequence()
        {
            Vector2 start = Position;
            while (true)
            {
                if (CanActivate() && HasPlayerRider())
                {
                    sfx.Play(_sfxPath, null, 0f);
                    ((IOriginalGameInputProcessEnvironment)Engine).InputSource.Rumble(RumbleStrength.Medium,
                        RumbleLength.Short);
                    StartShaking(0.1f);
                    yield return 0.1f;
                    streetlight.SetAnimationFrame(3);
                    StopPlayerRunIntoAnimation = false;
                    float at = 0f;
                    while (at < 1f)
                    {
                        yield return null;
                        Vector2 vector =
                            ZipMoverMotion.ForwardAtSpeed(Engine, start, target, SpeedMult, ref at, ref percent);
                        //this.ScrapeParticlesCheck(vector);
                        if (((ISourceScene)Scene).SourceScene.Clock.OnInterval(.1f, Engine))
                        {
                            CustomZipPathRenderer.Find(Scene)?.CreateSparks(this);
                        }

                        MoveTo(vector);
                    }

                    StartShaking(0.2f);
                    ((IOriginalGameInputProcessEnvironment)Engine).InputSource.Rumble(RumbleStrength.Strong,
                        RumbleLength.Medium);
                    ((OriginalLevelSceneRoot)Scene).Shake(0.3f);
                    StopPlayerRunIntoAnimation = true;
                    yield return 0.5f;
                    StopPlayerRunIntoAnimation = false;
                    streetlight.SetAnimationFrame(2);
                    at = 0f;
                    while (at < 1f)
                    {
                        yield return null;
                        Vector2 position = ZipMoverMotion.Backward(Engine, target, start, ref at, ref percent);
                        MoveTo(position);
                    }

                    StopPlayerRunIntoAnimation = true;
                    StartShaking(0.2f);
                    streetlight.SetAnimationFrame(1);
                    yield return 0.5f;
                }
                else
                {
                    yield return null;
                }
            }
        }

        private float mod(float x, float m)
        {
            return (x % m + m) % m;
        }

        public float SpeedMult, SpeedMultNormal, SpeedMultIce;

        //public float percentage;
    }
}
