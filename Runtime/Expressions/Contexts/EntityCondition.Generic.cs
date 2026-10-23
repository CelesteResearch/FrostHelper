using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Celeste.Mods.FrostHelper.Runtime.Expressions;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Conditions;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Commands;
using Session = Celeste.Simulation.Runtime.Session.ChapterCompletionSession;
using Entity = Celeste.Scene.Entities.SceneEntityState;

namespace Celeste.Mods.FrostHelper.Runtime.Expressions.Contexts
{
    internal abstract class EntityCondition<T> : Condition where T : Entity
    {
        protected abstract object GetValue(Session session, T entity);

        public override object Get(Session session, object? userdata) =>
            userdata is T entity ? GetValue(session, entity) : Zero;
    }
}
