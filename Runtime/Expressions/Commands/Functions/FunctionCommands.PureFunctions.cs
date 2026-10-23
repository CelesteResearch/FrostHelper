using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Reflection.Emit;
using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;
using Celeste.Mods.FrostHelper.Runtime.Expressions;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Conditions;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Compilation;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Descriptors;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Contexts;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Commands.Binding;
using Session = Celeste.Simulation.Runtime.Session.ChapterCompletionSession;
using PackedColor = Celeste.Core.Display.PackedColor;

namespace Celeste.Mods.FrostHelper.Runtime.Expressions.Commands
{
    internal sealed partial class FunctionCommands
    {
        private interface IPureFunc<in TArg1, out TRet>
        {
        }

        private interface ISessionFunc<in TArg1, out TRet>
        {
        }

        private interface IPureFunc<in TArg1, in TArg2, out TRet>
        {
        }

        private interface IPureFunc<in TArg1, in TArg2, in TArg3, out TRet>
        {
        }

        private interface IPureFunc<in TArg1, in TArg2, in TArg3, in TArg4, out TRet>
        {
        }

        private interface IPureMathFunc<T> : IPureFunc<T, T>
        {
        }

        private interface IPureTwoArgMathFunc<T> : IPureFunc<T, T, T>
        {
        }

        private interface IPureThreeArgMathFunc<T> : IPureFunc<T, T, T, T>
        {
        }

        private struct SinFunc : IPureMathFunc<float>
        {
            public static float Get(float x) => MathF.Sin(x);

            public static string ArgName => "x";
        }

        private struct CosFunc : IPureMathFunc<float>
        {
            public static float Get(float x) => MathF.Cos(x);

            public static string ArgName => "x";
        }

        private struct TanFunc : IPureMathFunc<float>
        {
            public static float Get(float x) => MathF.Tan(x);

            public static string ArgName => "x";
        }

        private struct TruncateFunc : IPureMathFunc<float>
        {
            public static float Get(float x) => MathF.Truncate(x);

            public static string ArgName => "x";
        }

        private struct FloorFunc : IPureMathFunc<float>
        {
            public static float Get(float x) => MathF.Floor(x);

            public static string ArgName => "x";
        }

        private struct CeilingFunc : IPureMathFunc<float>
        {
            public static float Get(float x) => MathF.Ceiling(x);

            public static string ArgName => "x";
        }

        private struct SqrtFunc : IPureMathFunc<float>
        {
            public static float Get(float x) => MathF.Sqrt(x);

            public static string ArgName => "x";
        }

        private struct CbrtFunc : IPureMathFunc<float>
        {
            public static float Get(float x) => MathF.Cbrt(x);

            public static string ArgName => "x";
        }

        private struct ExpFunc : IPureMathFunc<float>
        {
            public static float Get(float x) => MathF.Exp(x);

            public static string ArgName => "x";
        }

        private struct Exp2Func : IPureMathFunc<float>
        {
            public static float Get(float x) => MathF.Pow(2, x);

            public static string ArgName => "x";
        }

        private struct LognFunc : IPureMathFunc<float>
        {
            public static float Get(float x) => MathF.Log(x);

            public static string ArgName => "x";
        }

        private struct Log10Func : IPureMathFunc<float>
        {
            public static float Get(float x) => MathF.Log10(x);

            public static string ArgName => "x";
        }

        private struct Log2Func : IPureMathFunc<float>, IOperationMethodProvider
        {
            public MethodInfo ImplementationMethod => ScalarFunctionCompatibility.Log2Method;
            public static float Get(float x) => ScalarFunctionCompatibility.Log2(x);

            public static string ArgName => "x";
        }

        private struct RoundFunc : IPureMathFunc<float>
        {
            public static float Get(float x) => MathF.Round(x);

            public static string ArgName => "x";
        }

        private struct AbsFunc : IPureMathFunc<float>
        {
            public static float Get(float x) => MathF.Abs(x);

            public static string ArgName => "x";
        }

        private struct PowFunc : IPureTwoArgMathFunc<float>
        {
            public static float Get(float x, float y) => MathF.Pow(x, y);

            public static string Arg1Name => "x";

            public static string Arg2Name => "y";
        }

        private struct Pow2Func : IPureMathFunc<float>
        {
            public static float Get(float x) => x * x;

            public static string ArgName => "x";
        }

        private struct LogFunc : IPureTwoArgMathFunc<float>
        {
            public static float Get(float x, float y) => MathF.Log(x, y);

            public static string Arg1Name => "x";

            public static string Arg2Name => "y";
        }

        private struct LerpFunc : IPureThreeArgMathFunc<float>
        {
            public static float Get(float x, float y, float z) => (x * (1f - z)) + (y * z);

            public static string Arg1Name => "x";

            public static string Arg2Name => "y";

            public static string Arg3Name => "amount";
        }

        private struct LerpCFunc : IPureFunc<PackedColor, PackedColor, float, PackedColor>
        {
            public static PackedColor Get(PackedColor x, PackedColor y, float z) => PackedColor.Lerp(x, y, z);

            public static string Arg1Name => "source";

            public static string Arg2Name => "destination";

            public static string Arg3Name => "amount";
        }

        private struct YoYoFunc : IPureMathFunc<float>
        {
            public static float Get(float x) => Celeste.Core.Mathematics.ScalarMath.YoYo(x);

            public static string ArgName => "x";
        }

        private struct FlagsFunc : ISessionFunc<string, IEnumerable<string>>
        {
            public static IEnumerable<string> Get(Session session, string regex, FrostConditionRuntime runtime)
            {
                return session.Flags.Where(f => InstanceFunctionCommands.StringIsMatch.Invoke(f, regex, runtime) > 0
                );
            }

            public static string ArgName => "regex";
        }

        private struct RgbFunc : IPureFunc<int, int, int, PackedColor>
        {
            public static PackedColor Get(int r, int g, int b)
            {
                return new PackedColor(r, g, b);
            }

            public static string Arg1Name => "r";

            public static string Arg2Name => "g";

            public static string Arg3Name => "b";
        }

        private struct RgbaFunc : IPureFunc<int, int, int, int, PackedColor>
        {
            public static PackedColor Get(int r, int g, int b, int alpha)
            {
                return new PackedColor(r, g, b, alpha);
            }

            public static string Arg1Name => "r";

            public static string Arg2Name => "g";

            public static string Arg3Name => "b";

            public static string Arg4Name => "a";
        }

        private struct HsvFunc : IPureFunc<float, float, float, PackedColor>
        {
            public static PackedColor Get(float h, float s, float v)
            {
                return Celeste.Core.Display.ColorConversion.HsvToColor(h, s, v);
            }

            public static string Arg1Name => "h";

            public static string Arg2Name => "s";

            public static string Arg3Name => "v";
        }

        private struct HsvaFunc : IPureFunc<float, float, float, int, PackedColor>
        {
            public static PackedColor Get(float h, float s, float v, int alpha)
            {
                var color = Celeste.Core.Display.ColorConversion.HsvToColor(h, s, v);
                color.A = unchecked((byte)alpha);
                return color;
            }

            public static string Arg1Name => "h";

            public static string Arg2Name => "s";

            public static string Arg3Name => "v";

            public static string Arg4Name => "alpha";
        }
    }
}
