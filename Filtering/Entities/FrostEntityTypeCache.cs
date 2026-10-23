// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Modules.Updating;
using Celeste.Everest.Reflection;
using Celeste.Everest.Reflection.Runtime;

namespace Celeste.Mods.FrostHelper.Filtering.Entities
{
    internal sealed class FrostEntityTypeCache
    {
        private static readonly ConditionalWeakTable<SourceEverestReflectionProcessState, FrostEntityTypeCache> Caches =
            new();

        private long revision = -1;
        private long discoveryRevision = -1;
        internal Dictionary<string, Type> EntityNameTypes { get; private set; }

        internal static FrostEntityTypeCache For(SourceEverestReflectionProcessState reflection,
            SourceRuntimeTypeDiscovery discovery)
        {
            var cache = Caches.GetValue(reflection, _ => new());
            long revision = reflection.SourceMembers.ActualTypeBindingsRevision;
            if (cache.revision == revision && cache.discoveryRevision == discovery.Revision) return cache;
            var types = new Dictionary<string, Type>(StringComparer.Ordinal);
            foreach (var type in reflection.GetFakeEntryAssembly().GetTypes())
            {
                if (reflection.SourceMembers.ContainsSourceType(type))
                {
                    foreach (string id in EverestLoaderProcess.ReadSourceCustomEntityIds(
                                 reflection.SourceMembers.GetOriginalTypeDefinition(type))) Add(id, type);
                }
                else
                {
                    foreach (var attribute in type.GetCustomAttributes<CustomEntityAttribute>())
                    foreach (string id in attribute.IDs)
                        Add(id, type);
                }
            }

            cache.EntityNameTypes = types;
            cache.revision = revision;
            cache.discoveryRevision = discovery.Revision;
            return cache;

            void Add(string id, Type type)
            {
                string[] parts = id.Split('=');
                if (parts.Length is 1 or 2) types[parts[0].Trim()] = type;
            }
        }
    }
}
