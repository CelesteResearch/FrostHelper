using System;
using MonoMod.ModInterop;

namespace Celeste.Mods.FrostHelper.Runtime.Expressions.Persistence
{
    [ModImportName("SpeedrunTool.SaveLoad")]
    public sealed class ExpressionSavestateImports
    {
#pragma warning disable CS0649
        public Action<Func<Type, bool>> AddReturnSameObjectProcessor;
        public Action<Func<object, object>> AddCustomDeepCloneProcessor;
#pragma warning restore CS0649
    }
}
