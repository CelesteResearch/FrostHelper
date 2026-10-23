// SPDX-License-Identifier: MIT

using System;
using Celeste.Everest;

namespace Celeste.Mods.FrostHelper.Presentation.Notifications
{
    public record struct FrostNotification(EverestLogLevel Level, string Message)
    {
        internal DateTime Time;
        internal float Alpha = 1f;
        internal string Source = null;
    }
}
