using Celeste.Mods.FrostHelper.Runtime.Expressions.Persistence;
using Celeste.Game.Execution.Levels;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Conditions;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Conditions.Consts;
using Celeste.Scene.Tracking;
using Session = Celeste.Simulation.Runtime.Session.ChapterCompletionSession;

namespace Celeste.Mods.FrostHelper.Runtime.Expressions.Compilation
{
    internal sealed class SessionExpression<T> : ISavestatePersisted
    {
        private readonly CompiledCondition<T> condition;

        public SessionExpression(T constantValue)
        {
            PositiveValue<T>.PrepareIfNumeric();
            IsConstant = true;
            ConstantValue = constantValue;
            IsNotEmpty = true;
        }

        public SessionExpression(Condition source, FrostConditionRuntime runtime)
        {
            if (source is IConstCondition<T> constant)
            {
                PositiveValue<T>.PrepareIfNumeric();
                IsConstant = true;
                ConstantValue = constant.Value;
            }
            else condition = CompiledCondition<T>.GetFor(source, runtime);

            IsNotEmpty = !source.Empty;
        }

        public bool IsNotEmpty { get; }
        public bool IsConstant { get; }
        public T ConstantValue { get; }

        public T Get(ISourceScene scene) => condition is null
            ? ConstantValue
            : condition.Get(((OriginalLevelSceneRoot)scene).Session, null);

        public T Get(Session session) => condition is null ? ConstantValue : condition.Get(session, null);

        public T Get(Session session, object userdata) =>
            condition is null ? ConstantValue : condition.Get(session, userdata);
    }
}
