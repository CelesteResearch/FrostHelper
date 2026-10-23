// SPDX-License-Identifier: MIT

using System.Numerics;
using Celeste.Scene.Entities;
using Celeste.Simulation.Gameplay.Platforms.Foundation;

namespace Celeste.Mods.FrostHelper.Movement.Entities
{
    public static class FrostEntityMovement
    {
        public static void MoveEntity(SceneEntityState entity, Vector2 position)
        {
            if (entity is SourceSolidEntityState solid)
            {
                try
                {
                    solid.MoveTo(position);
                }
                catch
                {
                    // The fixed Frost helper swallows only Solid.MoveTo exceptions.
                }
            }
            else entity.Position = position;
        }
    }
}
