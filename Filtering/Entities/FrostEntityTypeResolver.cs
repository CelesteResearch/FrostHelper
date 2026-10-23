// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using Celeste.Everest.Reflection;
using Celeste.Everest.Reflection.Runtime;
using Celeste.Core.Scheduling;

namespace Celeste.Mods.FrostHelper.Filtering.Entities
{
    public sealed partial class FrostEntityTypeResolver
    {
        private readonly SourceEverestReflectionProcessState reflection;
        private readonly SourceRuntimeTypeDiscovery discovery;

        public FrostEntityTypeResolver(ISourceEngineContext engine) : this(
            ((ISourceEverestReflectionEnvironment)engine).Reflection,
            ((ISourceRuntimeTypeDiscoveryEnvironment)engine).RuntimeTypeDiscovery)
        {
        }

        public FrostEntityTypeResolver(SourceEverestReflectionProcessState reflection,
            SourceRuntimeTypeDiscovery discovery)
        {
            this.reflection = reflection ?? throw new ArgumentNullException(nameof(reflection));
            this.discovery = discovery ?? throw new ArgumentNullException(nameof(discovery));
        }

        public Type Resolve(string name)
        {
            var assembly = reflection.GetFakeEntryAssembly();
            var cache = FrostEntityTypeCache.For(reflection, discovery);
            if (cache.EntityNameTypes.TryGetValue(name, out var known)) return known;
            if (Aliases.TryGetValue(name, out var sourceName)) return assembly.GetType(sourceName, false, false);
            var result = assembly.GetType(name, false, true);
            if (result != null) cache.EntityNameTypes[name] = result;
            return result;
        }

        public Type[] DefaultBlacklist() => new[]
        {
            RequireOriginal("Player"), RequireOriginal("SolidTiles"), RequireOriginal("BackgroundTiles"),
            RequireOriginal("SpeedrunTimerDisplay"), RequireOriginal("StrawberriesCounter")
        };

        private Type RequireOriginal(string name) => reflection.GetFakeEntryAssembly().GetType("Celeste." + name)
                                                     ?? throw new InvalidOperationException(
                                                         "Missing actual original type binding: Celeste." + name);
    }
}
