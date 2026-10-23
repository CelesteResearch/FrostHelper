// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Collections;
using Celeste.Core.Components;
using Celeste.Core.Display;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Content.Textures;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Startup;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Content.Tiles;
using Celeste.Graphics.Content.Tiles.Autotiling;
using Celeste.Mods.FrostHelper.Runtime.Colors;
using Celeste.Mods.FrostHelper.Runtime.Spinners;
using Celeste.Scene.Entities;
using Celeste.Scene.Scheduling;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;

namespace Celeste.Mods.FrostHelper.Entities.Controllers.Colors
{
    [CustomEntity("FrostHelper/RainbowTilesetController = Load")]
    [RuntimeTypeAlias("FrostHelper.RainbowTilesetController")]
    [SceneTracked(false)]
    public sealed partial class RainbowTilesetController : SceneEntityState
    {
        private readonly HashSet<string> tilesetTextures;
        private readonly HashSet<char> debris;
        private readonly RainbowTilesetHooks hooks;
        private AtlasEntry lastChecked;
        private bool allDebris;

        public RainbowTilesetController(EntityData data, Vector2 offset, ISourceEngineContext engine,
            FrostSpinnerServices services) : base(
            ((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration,
            data.Position + offset)
        {
            hooks = RainbowTilesetHooks.For(engine, services);
            bool background = data.Bool("bg", false);
            bool all = data.Attr("tilesets", "") == "*";
            var resources = (ISourceAutotilingResources)engine;
            Autotiler autotiler = background ? resources.BackgroundAutotiler : resources.ForegroundAutotiler;
            bool includeDebris = data.Bool("includeDebris", false);
            AddTag(((IOriginalGameTagsEnvironment)engine).Tags.Persistent.Value);
            allDebris = all && includeDebris;
            if (!all)
            {
                string[] parts = data.Attr("tilesets", "").Trim().Split(',');
                var ids = new char[parts.Length];
                for (int i = 0; i < parts.Length; i++) ids[i] = parts[i][0];
                debris = includeDebris ? new HashSet<char>(ids) : new HashSet<char>();
                tilesetTextures = new HashSet<string>(ids.Length, StringComparer.Ordinal);
                for (int i = 0; i < ids.Length; i++) AddTileset(autotiler, ids[i]);
                return;
            }

            var terrainIds = autotiler.TerrainIds;
            tilesetTextures = new HashSet<string>(terrainIds.Count, StringComparer.Ordinal);
            var enumerator = terrainIds.GetEnumerator();
            for (int i = 0; i < terrainIds.Count; i++)
            {
                enumerator.MoveNext();
                AddTileset(autotiler, enumerator.Current);
            }

            debris = new HashSet<char>();
        }

        private void AddTileset(Autotiler autotiler, char id)
        {
            var tile = autotiler.GenerateMap(new VirtualMap<char>(new[,] { { id } }), true).TileGrid.Tiles[0, 0];
            string path = AtlasPath(((EverestAtlasEntry)tile).Parent);
            if (path != null) tilesetTextures.Add(path);
        }

        private static string AtlasPath(AtlasEntry texture) => texture?.AtlasPath ??
                                                               (texture as EverestAtlasEntry)?.Parent?.AtlasPath;

        public override void Awake(object scene)
        {
            base.Awake(scene);
            var controllers =
                ((ISourceScene)Scene).SourceScene.Entities.GetTrackedEntities(typeof(RainbowTilesetController));
            if (controllers.Count <= 1) return;
            RainbowTilesetController first = null;
            for (int i = 0; i < controllers.Count; i++)
                if (ReferenceEquals(((RainbowTilesetController)controllers[i]).Scene, scene))
                {
                    first = (RainbowTilesetController)controllers[i];
                    break;
                }

            if (first == null) throw new InvalidOperationException("Sequence contains no matching element");
            if (ReferenceEquals(first, this)) return;
            first.tilesetTextures.UnionWith(tilesetTextures);
            first.debris.UnionWith(debris);
            first.allDebris |= allDebris;
            RemoveSelf();
        }

        internal bool IncludesDebris(char tileset) => allDebris || debris.Contains(tileset);

        internal PackedColor GetColor(PackedColor original, Vector2 position, AtlasEntry texture, TileGrid grid)
        {
            AtlasEntry parent = ((EverestAtlasEntry)texture).Parent;
            if (ReferenceEquals(parent, lastChecked)) return hooks.GetHue(position) * grid.Alpha;
            string path = parent.AtlasPath;
            if (path != null && tilesetTextures.Contains(path))
            {
                lastChecked = parent;
                return hooks.GetHue(position) * grid.Alpha;
            }

            return original;
        }
    }
}
