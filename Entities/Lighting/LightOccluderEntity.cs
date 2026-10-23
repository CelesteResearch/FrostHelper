using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Geometry;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Scene.Entities;
using Celeste.Simulation.Presentation.Lighting;

namespace Celeste.Mods.FrostHelper.Entities.Lighting
{
    [CustomEntity("FrostHelper/LightOccluderEntity")]
    [RuntimeTypeAlias("FrostHelper.LightOccluderEntity")]
    public class LightOccluderEntity : SceneEntityState
    {
        public LightOccluderEntity(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
            Add(new LightOccludeState(new IntRect(0, 0, data.Width, data.Height), data.Float("alpha", 1f)));
        }
    }
}
