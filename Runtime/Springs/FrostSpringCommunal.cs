using System;
using MonoMod.ModInterop;

namespace Celeste.Mods.FrostHelper.Runtime.Springs
{
    [ModImportName("CommunalHelper.DashStates")]
    internal sealed class FrostSpringCommunal
    {
        public Func<int> GetDreamTunnelDashState = null;
    }
}
