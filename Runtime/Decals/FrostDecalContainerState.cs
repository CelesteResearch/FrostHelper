// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Celeste.Core.Scheduling;

namespace Celeste.Mods.FrostHelper.Runtime.Decals
{
    internal sealed class FrostDecalContainerState
    {
        private static readonly ConditionalWeakTable<ISourceEngineContext, FrostDecalContainerState> States = new();
        internal readonly HashSet<string> IgnoredNames = new();

        internal static FrostDecalContainerState For(ISourceEngineContext engine) =>
            States.GetValue(engine, static _ => new());
    }
}
