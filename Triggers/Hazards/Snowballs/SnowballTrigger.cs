using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Mods.FrostHelper.Entities.Hazards.Snowballs;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Triggers.Foundation;

namespace Celeste.Mods.FrostHelper.Triggers.Hazards.Snowballs
{
    [CustomEntity("FrostHelper/SnowballTrigger")]
    [RuntimeTypeAlias("FrostHelper.SnowballTrigger")]
    public sealed class SnowballTrigger : SourceTriggerEntityState
    {
        private readonly ISourceEngineContext engine;
        private readonly float speed, resetTime, sineWaveFrequency, safeZoneSize, offset;
        private readonly bool drawOutline, replaceExisting, once;
        private readonly string spritePath;
        private readonly CustomSnowball.AppearDirection appearDirection;

        public SnowballTrigger(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration,
                data.Position + offset, data.Width, data.Height, ((ISourceExtremaEnvironment)engine).ExtremaArithmetic)
        {
            this.engine = engine;
            speed = data.Float("speed", 200f);
            resetTime = data.Float("resetTime", .8f);
            drawOutline = data.Bool("drawOutline", true);
            spritePath = data.Attr("spritePath", "snowball");
            sineWaveFrequency = data.Float("ySineWaveFrequency", .5f);
            appearDirection = data.Enum("direction", CustomSnowball.AppearDirection.Right);
            replaceExisting = data.Bool("replaceExisting", true);
            safeZoneSize = data.Float("safeZoneSize", 64f);
            this.offset = data.Float("offset", 0f);
            once = data.Bool("once", true);
        }

        public override void OnEnter(RoomPlayerSession player)
        {
            base.OnEnter(player);
            var scene = ((ISourceScene)Scene).SourceScene;
            CustomSnowball current = replaceExisting ? scene.Entities.GetTrackedEntity<CustomSnowball>() : null;
            if (current != null)
            {
                current.Speed = speed;
                current.ResetTime = resetTime;
                current.Sine.Frequency = sineWaveFrequency;
                if (current.Sprite.Path != spritePath) current.CreateSprite(spritePath);
                current.DrawOutline = drawOutline;
                current.appearDirection = appearDirection;
                current.SafeZoneSize = safeZoneSize;
            }
            else
                scene.AddEntity(new CustomSnowball(engine, spritePath, speed, resetTime, sineWaveFrequency,
                    drawOutline, appearDirection, safeZoneSize, offset));

            if (once) RemoveSelf();
        }
    }
}
