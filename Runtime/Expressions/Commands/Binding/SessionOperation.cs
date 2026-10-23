using System;
using Session = Celeste.Simulation.Runtime.Session.ChapterCompletionSession;

namespace Celeste.Mods.FrostHelper.Runtime.Expressions.Commands.Binding
{
    internal static class SessionOperation<TArg, TRet, TOp>
    {
        public static readonly Func<Session, TArg, FrostConditionRuntime, TRet> Invoke =
            (Func<Session, TArg, FrostConditionRuntime, TRet>)OperationMetadata<TOp>.GetMethod.CreateDelegate(
                typeof(Func<Session, TArg, FrostConditionRuntime, TRet>));
    }
}
