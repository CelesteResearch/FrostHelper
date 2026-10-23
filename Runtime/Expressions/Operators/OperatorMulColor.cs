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

    internal struct OperatorMulColor : ITypeByNumberMathOperatorLeft<Color>, ITypeByNumberMathOperatorRight<Color>
    {
        public static Color Perform(Color a, float b)
        {
            return a * b;
        }

        public static Color Perform(float a, Color b)
        {
            return b * a;
        }

        public static Color Perform(int a, Color b)
        {
            return b * a;
        }

        public static Color Perform(Color a, int b)
        {
            return a * b;
        }

        public static OpCode? PerformOpCode => null;

        public static bool CanUseOpCodeFor(Condition a, Condition b)
        {
            return false;
        }
    }
}
