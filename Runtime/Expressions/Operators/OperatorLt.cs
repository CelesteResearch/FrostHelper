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

    internal struct OperatorLt : IComparisonOperator
    {
        public static bool Compare(int a, int b)
        {
            return a < b;
        }

        public static bool Compare(float a, float b)
        {
            return a < b;
        }

        public static bool Compare(string a, string b)
        {
            return a.AsSpan().CompareTo(b.AsSpan(), StringComparison.InvariantCulture) < 0;
        }

        public static List<OpCode> OpCodeSequence { get; } = [OpCodes.Clt];
    }
}
