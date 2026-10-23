using System.Runtime.CompilerServices;
using Celeste.Core.Scheduling;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Atlas;
using Celeste.Mods.FrostHelper.Entities.Controllers.BadelineChasers;

namespace Celeste.Mods.FrostHelper.Runtime.BadelineChasers
{
    internal sealed class BadelineChaserBlockResources
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, BadelineChaserBlockResources> Owners = new();

        internal static BadelineChaserBlockResources For(ISourceEngineContext engine) =>
            Owners.GetValue(engine, static e => new());

        internal AtlasEntry[,] Solid, Pressed, ActivatorSolid, ActivatorField;
        internal BadelineChaserBlockManager PendingManager;

        internal void Load(ISourceEngineContext engine)
        {
            var atlas = ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas;
            var solid = atlas["objects/FrostHelper/badelineChaserBlock/solid"];
            var pressed = atlas["objects/FrostHelper/badelineChaserBlock/pressed"];
            Pressed = new AtlasEntry[3, 3];
            Solid = new AtlasEntry[3, 3];
            for (int x = 0; x < 3; x++)
            for (int y = 0; y < 3; y++)
            {
                Pressed[x, y] = pressed.GetSubtexture(x * 8, y * 8, 8, 8);
                Solid[x, y] = solid.GetSubtexture(x * 8, y * 8, 8, 8);
            }

            var activatorSolid = atlas["objects/FrostHelper/badelineChaserBlock/activator"];
            ActivatorSolid = new AtlasEntry[3, 3];
            var activatorField = atlas["objects/FrostHelper/badelineChaserBlock/activatorfield"];
            ActivatorField = new AtlasEntry[3, 3];
            for (int x = 0; x < 3; x++)
            for (int y = 0; y < 3; y++)
            {
                ActivatorSolid[x, y] = activatorSolid.GetSubtexture(x * 8, y * 8, 8, 8);
                ActivatorField[x, y] = activatorField.GetSubtexture(x * 8, y * 8, 8, 8);
            }
        }
    }
}
