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

    internal sealed class MathOperatorRegistry
    {
        private readonly FrostConditionRuntime runtime;
        private readonly EmptyBinaryOp empty;

        internal MathOperatorRegistry(FrostConditionRuntime runtime)
        {
            this.runtime = runtime;
            empty = new EmptyBinaryOp { Runtime = runtime };
            Registry = new();

            RegisterDefaultMathOperator<int, OperatorAdd>(BinOpExpression.Operators.Add);
            RegisterDefaultMathOperator<int, OperatorSub>(BinOpExpression.Operators.Sub);
            RegisterDefaultMathOperator<int, OperatorDiv>(BinOpExpression.Operators.Div);
            RegisterDefaultMathOperator<float, OperatorDivFloat>(BinOpExpression.Operators.DivFloat);
            RegisterDefaultMathOperator<int, OperatorMul>(BinOpExpression.Operators.Mul);
            RegisterDefaultMathOperator<int, OperatorModulo>(BinOpExpression.Operators.Modulo);

            RegisterTypeByNumber<Color, OperatorMulColor>(BinOpExpression.Operators.Mul);
        }

        void Register<TLeft, TRight, TRes, TOperator>(BinOpExpression.Operators op)
            where TOperator : IMathOperator<TLeft, TRight, TRes>
        {
            Registry.TryAdd(op, new());

            var reg = Registry[op];
            reg[(typeof(TLeft), typeof(TRight))] = MathOperator<TLeft, TRight, TRes, TOperator>.Create;
        }

        void RegisterTypeByNumber<TType, TOperator>(BinOpExpression.Operators op)
            where TOperator : ITypeByNumberMathOperatorLeft<TType>, ITypeByNumberMathOperatorRight<TType>
        {
            Registry.TryAdd(op, new());

            var reg = Registry[op];
            reg[(typeof(float), typeof(TType))] = MathOperator<float, TType, TType, TOperator>.Create;
            reg[(typeof(int), typeof(TType))] = MathOperator<int, TType, TType, TOperator>.Create;
            reg[(typeof(TType), typeof(float))] = MathOperator<TType, float, TType, TOperator>.Create;
            reg[(typeof(TType), typeof(int))] = MathOperator<TType, int, TType, TOperator>.Create;
        }

        void RegisterDefaultMathOperator<TIntIntResult, TOperator>(BinOpExpression.Operators op)
            where TOperator : IMathOperator<TIntIntResult>
        {
            Registry.TryAdd(op, new());

            var reg = Registry[op];
            reg[(typeof(int), typeof(int))] = MathOperator<int, int, TIntIntResult, TOperator>.Create;
            reg[(typeof(int), typeof(float))] = MathOperator<int, float, float, TOperator>.Create;
            reg[(typeof(float), typeof(int))] = MathOperator<float, int, float, TOperator>.Create;
            reg[(typeof(float), typeof(float))] = MathOperator<float, float, float, TOperator>.Create;
            reg[(typeof(Vector2), typeof(float))] = MathOperator<Vector2, float, Vector2, TOperator>.Create;
            reg[(typeof(float), typeof(Vector2))] = MathOperator<float, Vector2, Vector2, TOperator>.Create;
            reg[(typeof(Vector2), typeof(int))] = MathOperator<Vector2, int, Vector2, TOperator>.Create;
            reg[(typeof(int), typeof(Vector2))] = MathOperator<int, Vector2, Vector2, TOperator>.Create;
            reg[(typeof(Vector2), typeof(Vector2))] = MathOperator<Vector2, Vector2, Vector2, TOperator>.Create;

            reg[(typeof(object), typeof(object))] = (a, b) => new DynamicMathOperator(runtime, op, a, b);
        }

        internal Dictionary<BinOpExpression.Operators,
            Dictionary<(Type left, Type right), Func<Condition, Condition, BinaryOperator>>> Registry { get; }

        public BinaryOperator CreateFor(BinOpExpression.Operators op, Condition condA, Condition condB)
        {
            var leftT = condA.ReturnType ?? typeof(object);
            var rightT = condB.ReturnType ?? typeof(object);

            return CreateFor(op, condA, condB, leftT, rightT);
        }

        public BinaryOperator CreateFor(BinOpExpression.Operators op, Condition condA, Condition condB, Type leftT,
            Type rightT)
        {
            var available = Registry[op];

            if (leftT == typeof(object) || rightT == typeof(object))
            {
                leftT = typeof(object);
                rightT = typeof(object);
            }

            if (available.TryGetValue((leftT, rightT), out var factory))
            {
                return factory(condA, condB);
            }

            runtime.Notify(
                $"Cannot perform {op} between {TypeDescriptor.For(leftT)} and {TypeDescriptor.For(rightT)}.");
            return empty;
        }
    }
}
