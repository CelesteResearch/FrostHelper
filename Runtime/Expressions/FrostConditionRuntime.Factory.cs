using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Conditions;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Conditions.Consts;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Operators;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Contexts;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Syntax;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Formats.Map.Binary.Source;
using Celeste.Mods.FrostHelper.Runtime.Expressions.Compilation;

namespace Celeste.Mods.FrostHelper.Runtime.Expressions
{
    public sealed partial class FrostConditionRuntime
    {
        internal Condition EmptyCondition { get; } = new Conditions.EmptyCondition();
        internal Condition TrueCondition { get; } = new ConstInt(1);
        internal Condition FalseCondition { get; } = new ConstInt(0);

        public Condition GetCondition(EntityData data, string name, string def = "") =>
            Bind(GetConditionCore(data.Values, Context, name, def));

        public Condition GetCondition(EntityData data, IExpressionContext context, string name, string def = "") =>
            Bind(GetConditionCore(data.Values, context, name, def));

        public Condition GetCondition(BinaryPackerElement data, string name, string def = "") =>
            Bind(GetConditionCore(data.Attributes, Context, name, def));

        public Condition GetCondition(BinaryPackerElement data, IExpressionContext context, string name,
            string def = "") => Bind(GetConditionCore(data.Attributes, context, name, def));

        internal SessionExpression<T> GetExpression<T>(EntityData data, string name, string def = "") =>
            new(GetCondition(data, name, def), this);

        internal SessionExpression<T> GetExpression<T>(EntityData data, IExpressionContext context, string name,
            string def = "") => new(GetCondition(data, context, name, def), this);

        private Condition Bind(Condition condition)
        {
            condition.Runtime = this;
            return condition;
        }

        private bool TryCreate(AbstractExpression expression, IExpressionContext context,
            [NotNullWhen(true)] out Condition condition)
        {
            bool success = TryCreateSyntax(expression, context, out condition);
            if (condition != null) condition.Runtime = this;
            return success;
        }

        internal Condition CreateOrDefault(string txt, string defaultValue, IExpressionContext? ctx = null)
        {
            ctx ??= Context;

            if (TryCreate(txt, ctx, out var cond))
                return cond;
            if (TryCreate(defaultValue, ctx, out cond))
                return cond;

            Notify($"Default condition is malformed, this is a Frost Helper bug!\n{defaultValue}\n{new StackTrace()}");
            return EmptyCondition;
        }


        internal bool TryCreate(string str, IExpressionContext ctx, [NotNullWhen(true)] out Condition? condition)
        {
            if (string.IsNullOrWhiteSpace(str))
            {
                condition = EmptyCondition;
                return true;
            }

            if (Parser.TryParseCached(str, out var expr))
            {
                var ret = TryCreate(expr, ctx, out condition);
                condition?.SourceText = str;
                return ret;
            }

            condition = null;
            return false;
        }


        private bool CreateList(IList<AbstractExpression> args, IExpressionContext ctx, out List<Condition> conditions)
        {
            conditions = new List<Condition>(args.Count);
            foreach (var argExpr in args)
            {
                if (!TryCreate(argExpr, ctx, out var argCond))
                {
                    return false;
                }

                conditions.Add(argCond);
            }

            return true;
        }


        private bool TryCreateSyntax(AbstractExpression expr, IExpressionContext ctx,
            [NotNullWhen(true)] out Condition? condition)
        {
            switch (expr)
            {
                case SimpleCommandExpression simpleCmd when simpleCmd.Name.StartsWith("input."):
                    return InputCommands.TryParseInput(simpleCmd.Name["input.".Length..], out condition);
                case SimpleCommandExpression simpleCmd:
                {
                    var remaining = simpleCmd.Name;
                    List<string>? fields = null;
                    condition = null;
                    while (true)
                    {
                        // Try simple commands from the context
                        if (ctx.TryGetSimpleCommand(remaining, out var cond))
                        {
                            condition = cond;
                            break;
                        }

                        // Try simple commands
                        if (SimpleCommands.Registry.TryGetValue(remaining, out var simpleCommand))
                        {
                            condition = simpleCommand.Condition;
                            break;
                        }

                        var lastDotIdx = remaining.LastIndexOf('.');
                        if (lastDotIdx == -1)
                            break;
                        fields ??= [];
                        fields.Add(remaining[(lastDotIdx + 1)..]);
                        remaining = remaining[..lastDotIdx];
                    }

                    if (condition is null)
                    {
                        Notify($"Unknown use of the $ operator: {expr}");
                        return false;
                    }

                    while (fields?.Count > 0)
                    {
                        condition = FieldAccessCommands.Create(fields[^1], condition, ctx);
                        fields.RemoveAt(fields.Count - 1);
                    }

                    return true;
                }
                case GetSessionVariableExpression sessVarExpr:
                {
                    if (!TryCreate(sessVarExpr.Name, ctx, out var nameCond))
                    {
                        condition = null;
                        return false;
                    }

                    switch (sessVarExpr.VariableType)
                    {
                        case GetSessionVariableExpression.Types.Flag:
                            condition = new FlagAccessor(nameCond, inverted: false);
                            return true;
                        case GetSessionVariableExpression.Types.Counter:
                            condition = nameCond is ConstString { Value: var n }
                                ? new CounterAccessorCondition(n)
                                : new IndirectCounterAccessor(nameCond);
                            return true;
                        case GetSessionVariableExpression.Types.Slider:
                            condition = nameCond is ConstString { Value: var sn }
                                ? new SliderAccessorCondition(sn)
                                : new IndirectSliderAccessor(nameCond);
                            return true;
                    }

                    break;
                }
                case InvertExpression invertExpression:
                {
                    if (!TryCreate(invertExpression.Expression, ctx, out var invertCond))
                    {
                        condition = null;
                        return false;
                    }

                    if (invertCond is IInvertible invertible)
                        condition = invertible.CreateInverted();
                    else
                        condition = new OperatorInvert(invertCond);
                    return true;
                }
                case FunctionCommandExpression { Name: { } funcName, Arguments: { } args }:
                {
                    if (!CreateList(args, ctx, out var argConds))
                    {
                        condition = null;
                        return false;
                    }

                    return FunctionCommands.TryCreate(funcName, argConds, ctx, out condition);
                }
                case InterpolatedStringExpression { Arguments: { } strArgs }:
                {
                    if (!CreateList(strArgs, ctx, out var argConds))
                    {
                        condition = null;
                        return false;
                    }

                    condition = new StringInterpolationOperator(argConds);
                    return true;
                }
                case LiteralExpression<string> stringLit:
                    condition = new ConstString(stringLit.Value);
                    return true;
                case LiteralExpression<int> intLit:
                    condition = new ConstInt(intLit.Value);
                    return true;
                case LiteralExpression<float> floatLit:
                    condition = new ConstFloat(floatLit.Value);
                    return true;
                case BinOpExpression { Left: { } left, Right: { } right } binExpr:
                {
                    if (!TryCreate(left, ctx, out var leftExpr))
                    {
                        condition = null;
                        return false;
                    }

                    if (!TryCreate(right, ctx, out var rightExpr))
                    {
                        condition = null;
                        return false;
                    }

                    condition = binExpr.Operator switch
                    {
                        BinOpExpression.Operators.And => new OperatorAnd(leftExpr, rightExpr),
                        BinOpExpression.Operators.Or => new OperatorOr(leftExpr, rightExpr),
                        BinOpExpression.Operators.BitwiseAnd => new BitwiseOperator<OperatorBitwiseAnd>(leftExpr,
                            rightExpr),
                        BinOpExpression.Operators.BitwiseOr => new BitwiseOperator<OperatorBitwiseOr>(leftExpr,
                            rightExpr),
                        BinOpExpression.Operators.Add => MathOperators.CreateFor(binExpr.Operator, leftExpr, rightExpr),
                        BinOpExpression.Operators.Sub => MathOperators.CreateFor(binExpr.Operator, leftExpr, rightExpr),
                        BinOpExpression.Operators.Mul => MathOperators.CreateFor(binExpr.Operator, leftExpr, rightExpr),
                        BinOpExpression.Operators.Div => MathOperators.CreateFor(binExpr.Operator, leftExpr, rightExpr),
                        BinOpExpression.Operators.DivFloat => MathOperators.CreateFor(binExpr.Operator, leftExpr,
                            rightExpr),
                        BinOpExpression.Operators.Modulo => MathOperators.CreateFor(binExpr.Operator, leftExpr,
                            rightExpr),
                        BinOpExpression.Operators.Lt => new ComparisonOperator<OperatorLt>(leftExpr, rightExpr),
                        BinOpExpression.Operators.Gt => new ComparisonOperator<OperatorGt>(leftExpr, rightExpr),
                        BinOpExpression.Operators.Eq => new ComparisonOperator<OperatorEq>(leftExpr, rightExpr),
                        BinOpExpression.Operators.Ne => new ComparisonOperator<OperatorNe>(leftExpr, rightExpr),
                        BinOpExpression.Operators.Ge => new ComparisonOperator<OperatorGte>(leftExpr, rightExpr),
                        BinOpExpression.Operators.Le => new ComparisonOperator<OperatorLte>(leftExpr, rightExpr),
                        _ => null
                    };

                    if (condition is null)
                    {
                        Notify($"Unknown operator: {binExpr.Operator}");
                        return false;
                    }

                    return true;
                }
                case FieldAccessExpression
                {
                    Name: var fieldName, ObjectExpression: var objectExpression, Arguments: var arguments
                }:
                {
                    if (!TryCreate(objectExpression, ctx, out var objExpr))
                    {
                        condition = null;
                        return false;
                    }

                    if (arguments is { })
                    {
                        List<Condition> args = [];
                        foreach (var argExpr in arguments)
                        {
                            if (!TryCreate(argExpr, ctx, out var arg))
                            {
                                condition = null;
                                return false;
                            }

                            args.Add(arg);
                        }

                        condition = InstanceFunctionCommands.Create(fieldName, objExpr, args, ctx);
                        return true;
                    }

                    condition = FieldAccessCommands.Create(fieldName, objExpr, ctx);
                    return true;
                }
                case LambdaExpression lambda:
                {
                    var lambdaCondition = new LambdaDefinitionCondition(lambda.ArgumentNames);
                    var lambdaCtx = new LambdaContext(ctx, lambdaCondition);
                    if (!TryCreate(lambda.Code, lambdaCtx, out var codeCondition))
                    {
                        condition = null;
                        return false;
                    }

                    lambdaCondition.Code = codeCondition;

                    condition = lambdaCondition;
                    return true;
                }
            }

            Notify($"Couldn't parse: {expr}");
            condition = null;
            return false;
        }


        private Condition GetConditionCore(Dictionary<string, object>? dict, IExpressionContext ctx, string name,
            string def = "")
        {
            Condition? condition = null;
            if (dict?.TryGetValue(name, out var cond) ?? false)
            {
                switch (cond)
                {
                    case Condition fullCondition:
                        condition = fullCondition;
                        break;
                    case string str:
                        if (TryCreate(str, ctx, out condition))
                        {
                            // dict[name] = condition; // cache the parsed condition
                        }

                        break;
                    // If an update converts a previously-number field into a session expression string field,
                    // old .bins will still have numbers stored here and as such we should convert them.
                    case int i:
                        return new ConstInt(i);
                    case float f:
                        return new ConstFloat(f);
                    case bool b:
                        return new ConstInt(b ? 1 : 0);
                    default:
                        if (TryCreate(cond.ToString() ?? "", ctx, out condition))
                        {
                            // dict[name] = condition; // cache the parsed condition
                        }

                        break;
                }
            }

            if (condition is null && TryCreate(def, ctx, out condition))
            {
                return condition;
            }

            condition ??= EmptyCondition;
            return condition;
        }
    }
}
