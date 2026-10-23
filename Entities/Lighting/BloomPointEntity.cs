using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Scheduling;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Scene.Entities;
using Celeste.Simulation.Presentation.PostProcessing.Bloom;

namespace Celeste.Mods.FrostHelper.Entities.Lighting
{
    [CustomEntity("FrostHelper/BloomPoint")]
    [RuntimeTypeAlias("FrostHelper.BloomPointEntity")]
    public class BloomPointEntity : SceneEntityState
    {
        public BloomPointEntity(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration, data.Position + offset)
        {
            Add(new BloomPointState(data.Float("alpha", 1f), data.Float("radius", 16f)));
        }
    }
}
