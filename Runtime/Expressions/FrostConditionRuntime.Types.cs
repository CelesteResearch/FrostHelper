using System;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Commands.Types;

namespace Celeste.Mods.FrostHelper.Runtime.Expressions
{
    public sealed partial class FrostConditionRuntime
    {
        public Type[] GetTypes(string typeString)
        {
            if (typeString == string.Empty) return Type.EmptyTypes;
            var names = typeString.Trim().Split(',');
            var result = new Type[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                var name = names[i].Trim();
                var type = EntityTypeNames.EntityNameToTypeSafe(name);
                if (type is null)
                {
                    Notify("Unknown Entity Name: " + name);
                    type = typeof(EntityTypeNames);
                }

                result[i] = type;
            }

            return result;
        }
    }
}
