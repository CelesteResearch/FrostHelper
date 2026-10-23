using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Diagnostics.CodeAnalysis;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Operators;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Conditions;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Conditions.Consts;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Compilation;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Syntax;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Contexts;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Descriptors;
using Vector2 = System.Numerics.Vector2;
using Session = Celeste.Simulation.Runtime.Session.ChapterCompletionSession;

namespace Celeste.Mods.FrostHelper.Runtime.Expressions.Conditions
{
    using Color = Celeste.Core.Display.PackedColor;


    internal sealed class SliderAccessorCondition(string name) : Condition
    {
        private static readonly MethodInfo MethodSessionGetSlider =
            typeof(Session).GetMethod(nameof(Session.GetSlider), BindingFlags.Instance | BindingFlags.Public)!;

        private WeakReference<Session.Slider>? _slider;
        private WeakReference<Session>? _lastSession;

        public override object Get(Session session, object? userdata)
        {
            if ((_lastSession?.TryGetTarget(out var last) ?? false) && last != session)
            {
                _slider = null;
                _lastSession = null;
            }

            _lastSession ??= new WeakReference<Session>(session);

            if (_slider?.TryGetTarget(out var slider) is not true)
            {
                slider = session.GetSliderObject(name);
                _slider = new(slider);
            }

            return slider.Value;
        }

        internal override void Emit(ConditionCompilationCtx ctx, Type targetType)
        {
            ctx.EmitLoadSession();
            ctx.Il.Emit(OpCodes.Ldstr, name);
            ctx.Il.Emit(OpCodes.Callvirt, MethodSessionGetSlider);
            ctx.EmitConvertTo(typeof(float), targetType);
        }

        internal override bool UsesCurrentConditionLocalInEmit => false;

        protected internal override Type ReturnType => typeof(float);
    }
}
