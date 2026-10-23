using System;
using Session = Celeste.Simulation.Runtime.Session.ChapterCompletionSession;

namespace Celeste.Mods.FrostHelper.Runtime.Expressions.Commands.Binding
{
    internal static class OneArgOperation<TArg, TRet, TOp>
    {
        public static readonly Func<TArg, TRet> Invoke =
            (Func<TArg, TRet>)OperationMetadata<TOp>.GetMethod.CreateDelegate(typeof(Func<TArg, TRet>));
    }
}
