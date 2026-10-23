using System;
using Session = Celeste.Simulation.Runtime.Session.ChapterCompletionSession;

namespace Celeste.Mods.FrostHelper.Runtime.Expressions.Commands.Binding
{
    internal static class FourArgOperation<TArg1, TArg2, TArg3, TArg4, TRet, TOp>
    {
        public static readonly Func<TArg1, TArg2, TArg3, TArg4, TRet> Invoke =
            (Func<TArg1, TArg2, TArg3, TArg4, TRet>)OperationMetadata<TOp>.GetMethod.CreateDelegate(
                typeof(Func<TArg1, TArg2, TArg3, TArg4, TRet>));
    }
}
