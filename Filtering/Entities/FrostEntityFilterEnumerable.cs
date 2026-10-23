// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using Celeste.Scene.Entities;

namespace Celeste.Mods.FrostHelper.Filtering.Entities
{
    public readonly struct FrostEntityFilterEnumerable
    {
        private readonly IReadOnlyList<object> entities;
        private readonly FrostEntityFilter filter;

        public FrostEntityFilterEnumerable(IReadOnlyList<object> entities, FrostEntityFilter filter)
        {
            this.entities = entities;
            this.filter = filter;
        }

        public Enumerator GetEnumerator() => new Enumerator(entities, filter);

        public struct Enumerator
        {
            private readonly IReadOnlyList<object> entities;
            private readonly FrostEntityFilter filter;
            private readonly int length;
            private int index;
            public SceneEntityState Current { get; private set; }

            public Enumerator(IReadOnlyList<object> entities, FrostEntityFilter filter)
            {
                this.entities = entities;
                this.filter = filter;
                length = entities.Count;
                index = -1;
                Current = null;
            }

            public bool MoveNext()
            {
                while (++index < length)
                {
                    var entity = (SceneEntityState)entities[index];
                    if (!filter.Matches(entity)) continue;
                    Current = entity;
                    return true;
                }

                return false;
            }

            public void Reset() => index = -1;

            public void Dispose()
            {
            }
        }
    }
}
