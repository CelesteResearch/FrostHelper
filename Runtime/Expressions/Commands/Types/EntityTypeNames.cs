using System;
using System.Collections.Generic;
using System.Reflection;
using Celeste.Everest.Reflection;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Diagnostics.Logging;

namespace Celeste.Mods.FrostHelper.Runtime.Expressions.Commands.Types
{
    public sealed partial class EntityTypeNames : IDisposable
    {
        private readonly FrostConditionRuntime runtime;
        private Dictionary<string, Type>? entityNameToType;

        public EntityTypeNames(FrostConditionRuntime runtime)
        {
            this.runtime = runtime;
        }

        public void Load() => AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;
        public void Dispose() => AppDomain.CurrentDomain.AssemblyLoad -= OnAssemblyLoad;
        private void OnAssemblyLoad(object? sender, AssemblyLoadEventArgs args) => entityNameToType = null;

        public Type? EntityNameToTypeSafe(string entityName)
        {
            if (entityNameToType is null) CreateCache();
            if (entityNameToType!.TryGetValue(entityName, out var result)) return result;
            var reflection = ((ISourceEverestReflectionEnvironment)runtime.Engine).Reflection;
            var type = reflection.GetFakeEntryAssembly().GetType(entityName, false, true);
            if (type is not null) entityNameToType[entityName] = type;
            return type;
        }

        public string? TypeToEntityName(Type targetType)
        {
            var cached = entityNameToType;
            if (cached is null) return null;
            foreach (var entry in cached)
                if (entry.Value == targetType)
                    return entry.Key;
            return null;
        }

        public string? TypeNameToEntityName(string typeName)
        {
            var type = EntityNameToTypeSafe(typeName);
            return type is null ? null : TypeToEntityName(type);
        }

        private void CreateCache()
        {
            var reflection = ((ISourceEverestReflectionEnvironment)runtime.Engine).Reflection;
            var universe = reflection.GetFakeEntryAssembly().GetTypes();
            var sourceTypes = new Dictionary<string, Type>(StringComparer.Ordinal);
            foreach (var type in universe)
                if (reflection.SourceMembers.ContainsSourceType(type))
                    sourceTypes[reflection.SourceMembers.GetOriginalTypeDefinition(type).Identity] = type;
            entityNameToType = new Dictionary<string, Type>();
            foreach (var entry in VanillaEntries)
                if (sourceTypes.TryGetValue(entry.TypeName, out var type))
                    entityNameToType[entry.Id] = type;
            foreach (var type in universe)
            foreach (var attribute in type.GetCustomAttributes<CustomEntityAttribute>())
            foreach (var id in attribute.IDs)
            {
                var parts = id.Split('=');
                if (parts.Length is not (1 or 2)) continue;
                var name = parts[0].Trim();
                if (entityNameToType.TryGetValue(name, out var previous))
                    ((IEverestLoggerProcessEnvironment)runtime.Engine).Logger.Warn("FrostHelper.TypeHelper",
                        $"Found duplicate entity ID {name} - {type.FullName} vs {previous.FullName}");
                entityNameToType[name] = type;
            }
        }
    }
}
