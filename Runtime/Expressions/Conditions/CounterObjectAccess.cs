using Session = Celeste.Simulation.Runtime.Session.ChapterCompletionSession;

namespace Celeste.Mods.FrostHelper.Runtime.Expressions.Conditions
{
    internal static class CounterObjectAccess
    {
        internal static Session.Counter Get(Session session, string name)
        {
            foreach (var counter in session.Counters)
                if (counter.Key == name)
                    return counter;
            var created = new Session.Counter { Key = name, Value = 0 };
            session.Counters.Add(created);
            return created;
        }
    }
}
