// SPDX-License-Identifier: MIT

using System;
using MonoMod.ModInterop;

namespace Celeste.Mods.FrostHelper.Runtime.Boosters
{
    [ModImportName("GravityHelper")]
    public sealed class BoosterGravityImports
    {
        public Func<bool> IsPlayerInverted;
        public Action BeginOverride;
        public Action EndOverride;
        public Action<string> RegisterModSupportBlacklist;
    }
}
