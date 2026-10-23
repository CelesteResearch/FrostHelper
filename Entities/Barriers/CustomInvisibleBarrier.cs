// SPDX-License-Identifier: MIT

using System.Numerics;
using Celeste.Core.Reflection.Runtime;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Simulation.Gameplay.Collision.Blockers;
using Celeste.Simulation.Gameplay.Platforms.Foundation;

namespace Celeste.Mods.FrostHelper.Entities.Barriers
{
    [CustomEntity("FrostHelper/CustomInvisibleBarrier")]
    [RuntimeTypeAlias("FrostHelper.CustomInvisibleBarrier")]
    public sealed class CustomInvisibleBarrier : SourceSolidEntityState
    {
        public static CustomInvisibleBarrier Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) => new(data, offset, level.SolidWorldFactory);

        public CustomInvisibleBarrier(EntityData data, Vector2 offset, ISourceSolidWorldFactory worlds)
            : base(data.Position + offset, data.Width, data.Height, true, worlds.Create(data.Position + offset, true))
        {
            Visible = false;
            if (!data.Bool("canClimb")) Add(new ClimbBlockerState(true));
            SurfaceSoundIndex = data.Int("soundIndex", 33);
            AllowStaticMovers = false;
        }
    }
}
