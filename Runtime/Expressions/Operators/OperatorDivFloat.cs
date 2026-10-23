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

namespace Celeste.Mods.FrostHelper.Runtime.Expressions.Operators
{
    using Color = Celeste.Core.Display.PackedColor;

    internal struct OperatorDivFloat : IMathOperator<float>
    {
        public static Vector2 Perform(float a, Vector2 b)
        {
            return new Vector2(a / b.X, a / b.Y);
        }

        public static Vector2 Perform(Vector2 a, float b)
        {
            return new Vector2(a.X / b, a.Y / b);
        }

        public static Vector2 Perform(Vector2 a, Vector2 b)
        {
            return new Vector2(a.X / b.X, a.Y / b.Y);
        }

        public static float Perform(float a, float b)
        {
            if (b == 0)
                return 0;
            return a / b;
        }

        public static float Perform(float a, int b)
        {
            if (b == 0)
                return 0;
            return a / b;
        }

        public static float Perform(int a, float b)
        {
            if (b == 0)
                return 0;
            return a / b;
        }

        public static float Perform(int a, int b)
        {
            if (b == 0)
                return 0;
            return a / (float)b;
        }

        public static Vector2 Perform(int a, Vector2 b)
        {
            return new Vector2(a / b.X, a / b.Y);
        }

        public static Vector2 Perform(Vector2 a, int b)
        {
            return new Vector2(a.X / b, a.Y / b);
        }

        public static OpCode? PerformOpCode => OpCodes.Div;

        public static bool CanUseOpCodeFor(Condition a, Condition b)
        {
            return a.ReturnTypeIsNumber && b is IConstCondition<float> { Value: not 0 };
        }
    }
}
