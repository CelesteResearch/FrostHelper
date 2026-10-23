// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;

namespace Celeste.Mods.FrostHelper.Filtering.Entities
{
    public sealed class FrostEntityFilter
    {
        private readonly HashSet<Type> types;
        private readonly HashSet<int> ids;
        private readonly bool isBlacklist;

        public FrostEntityFilter(HashSet<Type> types, bool isBlacklist, HashSet<int> ids)
        {
            this.types = types;
            this.isBlacklist = isBlacklist;
            this.ids = ids;
        }

        public IReadOnlyCollection<Type> Types => types;
        public IReadOnlyCollection<int> Ids => ids;
        public bool Empty => !isBlacklist && types.Count == 0 && ids.Count == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Matches(SceneEntityState entity) =>
            (ids.Contains(entity.SourceId.ID) || types.Contains(entity.GetType())) != isBlacklist;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MatchesBackdrop(object backdrop) => types.Contains(backdrop.GetType()) != isBlacklist;

        public FrostEntityFilterEnumerable Filter(ISourceScene scene) =>
            new FrostEntityFilterEnumerable(scene.SourceScene.Entities.Entities, this);

        public static FrostEntityFilter CreateFrom(string value, bool isBlacklist,
            FrostEntityTypeResolver resolver, Type[] blacklistTypes = null, Action<string> notify = null)
        {
            var types = new HashSet<Type>();
            var ids = new HashSet<int>();
            string remaining = value.Trim();
            int start = 0;
            while (start < remaining.Length)
            {
                int end = remaining.IndexOf(',', start);
                if (end < 0) end = remaining.Length;
                string token = remaining.Substring(start, end - start).Trim();
                start = end + 1;
                if (int.TryParse(token, out int id)) ids.Add(id);
                else if (resolver.Resolve(token) is Type type) types.Add(type);
                else notify?.Invoke($"Couldn't parse '{token}' as an entity name or C# type.");
            }

            if (isBlacklist)
                foreach (var type in blacklistTypes ?? resolver.DefaultBlacklist())
                    types.Add(type);
            return new FrostEntityFilter(types, isBlacklist, ids);
        }

        public static FrostEntityFilter CreateFrom(EntityData data, FrostEntityTypeResolver resolver,
            string typesKey = "types", string blacklistKey = "blacklist", Type[] blacklistTypes = null,
            Action<string> notify = null) => CreateFrom(data.Attr(typesKey, ""), data.Bool(blacklistKey), resolver,
            blacklistTypes, notify);
    }
}
