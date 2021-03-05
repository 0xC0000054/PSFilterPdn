/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2021 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace PSFilterPdn.EnableInfo
{
    internal sealed class EnableInfoInterpreter : IExpressionVisitor
    {
        private readonly EnableInfoVariables variables;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnableInfoInterpreter"/> class.
        /// </summary>
        /// <param name="variables">The variables.</param>
        /// <exception cref="ArgumentNullException"><paramref name="variables"/> is null.</exception>
        public EnableInfoInterpreter(EnableInfoVariables variables)
        {
            this.variables = variables ?? throw new ArgumentNullException(nameof(variables));
        }

        /// <summary>
        /// Evaluates the specified enable info expression.
        /// </summary>
        /// <param name="node">The expression to evaluate.</param>
        /// <returns>A <see cref="bool"/> indicating the result of the enable info expression.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="node"/> is null.</exception>
        /// <exception cref="EnableInfoException">The specified expression is not valid.</exception>
        public bool Evaluate(Expression node)
        {
            if (node == null)
            {
                throw new ArgumentNullException(nameof(node));
            }

            BooleanConstantExpression result = Visit(node) as BooleanConstantExpression;

            if (result == null)
            {
                return false;
            }

            return result.Value;
        }

        private Expression Visit(Expression node)
        {
            return node?.Accept(this);
        }

        private static bool IsEqual(ConstantExpression left, ConstantExpression right)
        {
            if (left.ConstantType != right.ConstantType)
            {
                return false;
            }

            if (left.ConstantType == ConstantType.Boolean)
            {
                BooleanConstantExpression leftBoolean = (BooleanConstantExpression)left;
                BooleanConstantExpression rightBoolean = (BooleanConstantExpression)right;

                return leftBoolean.Value == rightBoolean.Value;
            }
            else if (left.ConstantType == ConstantType.Integer)
            {
                IntegerConstantExpression leftInteger = (IntegerConstantExpression)left;
                IntegerConstantExpression rightInteger = (IntegerConstantExpression)right;

                return leftInteger.Value == rightInteger.Value;
            }
            else if (left.ConstantType == ConstantType.String)
            {
                StringConstantExpression leftString = (StringConstantExpression)left;
                StringConstantExpression rightString = (StringConstantExpression)right;

                return string.Equals(leftString.Value, rightString.Value, StringComparison.OrdinalIgnoreCase);
            }
            else
            {
                throw new EnableInfoException(string.Format(CultureInfo.InvariantCulture, "{0} is not supported.", left.ConstantType));
            }
        }

        [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "ConstantExpression")]
        Expression IExpressionVisitor.VisitBinary(BinaryExpression node)
        {
            ConstantExpression left = Visit(node.Left) as ConstantExpression;
            ConstantExpression right = Visit(node.Right) as ConstantExpression;

            if (left == null || right == null)
            {
                throw new EnableInfoException(string.Format(CultureInfo.InvariantCulture,
                                                            "The binary expression operands must be a ConstantExpression. Left: {0} Right: {1}",
                                                            node.Left?.GetType().ToString() ?? "null", node.Right?.GetType().ToString() ?? "null"));
            }
            if (left.ConstantType == ConstantType.UndefinedVariable)
            {
                return left;
            }
            else if (right.ConstantType == ConstantType.UndefinedVariable)
            {
                return right;
            }

            if (node.NodeType == ExpressionType.Equal)
            {
                return new BooleanConstantExpression(IsEqual(left, right));
            }
            else if (node.NodeType == ExpressionType.NotEqual)
            {
                return new BooleanConstantExpression(!IsEqual(left, right));
            }
            else
            {
                if (left.ConstantType != ConstantType.Integer || right.ConstantType != ConstantType.Integer)
                {
                    throw new EnableInfoException(string.Format(CultureInfo.InvariantCulture,
                                                                "The arithmetic operands must be an Integer. Left: {0} Right: {1}",
                                                                left.ConstantType, right.ConstantType));
                }

                IntegerConstantExpression leftInteger = (IntegerConstantExpression)left;
                IntegerConstantExpression rightInteger = (IntegerConstantExpression)right;

                switch (node.NodeType)
                {
                    case ExpressionType.Add:
                        return new IntegerConstantExpression(leftInteger.Value + rightInteger.Value);
                    case ExpressionType.Divide:
                        if (rightInteger.Value == 0)
                        {
                            throw new EnableInfoException("Attempted to divide by zero.");
                        }
                        return new IntegerConstantExpression(leftInteger.Value / rightInteger.Value);
                    case ExpressionType.GreaterThan:
                        return new BooleanConstantExpression(leftInteger.Value > rightInteger.Value);
                    case ExpressionType.GreaterThanOrEqual:
                        return new BooleanConstantExpression(leftInteger.Value >= rightInteger.Value);
                    case ExpressionType.LessThan:
                        return new BooleanConstantExpression(leftInteger.Value < rightInteger.Value);
                    case ExpressionType.LessThanOrEqual:
                        return new BooleanConstantExpression(leftInteger.Value <= rightInteger.Value);
                    case ExpressionType.Multiply:
                        return new IntegerConstantExpression(leftInteger.Value * rightInteger.Value);
                    case ExpressionType.Subtract:
                        return new IntegerConstantExpression(leftInteger.Value - rightInteger.Value);
                    default:
                        throw new EnableInfoException(string.Format(CultureInfo.InvariantCulture, "{0} is not a supported binary operator.", node.NodeType));
                }
            }
        }

        Expression IExpressionVisitor.VisitConstant(ConstantExpression node)
        {
            return node;
        }

        Expression IExpressionVisitor.VisitFunctionCall(FunctionCallExpression node)
        {
            if (string.Equals(node.Name, "in", StringComparison.OrdinalIgnoreCase))
            {
                return VisitInFunction(node.Arguments);
            }
            else if (string.Equals(node.Name, "min", StringComparison.OrdinalIgnoreCase))
            {
                return VisitMinFunction(node.Arguments);
            }
            else if (string.Equals(node.Name, "max", StringComparison.OrdinalIgnoreCase))
            {
                return VisitMaxFunction(node.Arguments);
            }
            else if (string.Equals(node.Name, "dim", StringComparison.OrdinalIgnoreCase))
            {
                return VisitDimFunction(node.Arguments);
            }
            else
            {
                throw new EnableInfoException(string.Format(CultureInfo.InvariantCulture, "Unknown function '{0}'.", node.Name));
            }
        }

        private BooleanConstantExpression VisitInFunction(ReadOnlyCollection<Expression> args)
        {
            if (args != null && args.Count >= 2)
            {
                // The EnableInfo documentation states that the 'in' function returns true
                // if the first parameter is equal to at least one of the following parameters.

                ConstantExpression first = Visit(args[0]) as ConstantExpression;
                if (first != null)
                {
                    if (first.ConstantType == ConstantType.String)
                    {
                        string firstParameter = ((StringConstantExpression)first).Value;

                        for (int i = 1; i < args.Count; i++)
                        {
                            StringConstantExpression parameter = Visit(args[i]) as StringConstantExpression;
                            if (parameter != null && string.Equals(firstParameter, parameter.Value, StringComparison.OrdinalIgnoreCase))
                            {
                                return BooleanConstantExpression.True;
                            }
                        }
                    }
                    else if (first.ConstantType == ConstantType.Integer)
                    {
                        int firstParameter = ((IntegerConstantExpression)first).Value;

                        for (int i = 1; i < args.Count; i++)
                        {
                            IntegerConstantExpression parameter = Visit(args[i]) as IntegerConstantExpression;
                            if (parameter != null && firstParameter == parameter.Value)
                            {
                                return BooleanConstantExpression.True;
                            }
                        }
                    }
                    else if (first.ConstantType == ConstantType.Boolean)
                    {
                        bool firstParameter = ((BooleanConstantExpression)first).Value;

                        for (int i = 1; i < args.Count; i++)
                        {
                            BooleanConstantExpression parameter = Visit(args[i]) as BooleanConstantExpression;
                            if (parameter != null && firstParameter == parameter.Value)
                            {
                                return BooleanConstantExpression.True;
                            }
                        }
                    }
                }
            }

            return BooleanConstantExpression.False;
        }

        private IntegerConstantExpression VisitMinFunction(ReadOnlyCollection<Expression> args)
        {
            int min = 0;

            if (args != null && args.Count >= 2)
            {
                IntegerConstantExpression first = Visit(args[0]) as IntegerConstantExpression;
                IntegerConstantExpression second = Visit(args[1]) as IntegerConstantExpression;

                if (first != null && second != null)
                {
                    min = Math.Min(first.Value, second.Value);

                    for (int i = 2; i < args.Count; i++)
                    {
                        IntegerConstantExpression parameter = Visit(args[i]) as IntegerConstantExpression;
                        if (parameter != null)
                        {
                            min = Math.Min(min, parameter.Value);
                        }
                    }
                }
            }

            return new IntegerConstantExpression(min);
        }

        private IntegerConstantExpression VisitMaxFunction(ReadOnlyCollection<Expression> args)
        {
            int max = 0;

            if (args != null && args.Count >= 2)
            {
                IntegerConstantExpression first = Visit(args[0]) as IntegerConstantExpression;
                IntegerConstantExpression second = Visit(args[1]) as IntegerConstantExpression;

                if (first != null && second != null)
                {
                    max = Math.Max(first.Value, second.Value);

                    for (int i = 2; i < args.Count; i++)
                    {
                        IntegerConstantExpression parameter = Visit(args[i]) as IntegerConstantExpression;
                        if (parameter != null)
                        {
                            max = Math.Max(max, parameter.Value);
                        }
                    }
                }
            }

            return new IntegerConstantExpression(max);
        }

        private BooleanConstantExpression VisitDimFunction(ReadOnlyCollection<Expression> args)
        {
            BooleanConstantExpression result = BooleanConstantExpression.False;

            if (args != null && args.Count == 2)
            {
                // The EnableInfo documentation does not provide any information on how the 'dim' function is used.
                // After using a filter to test the 'dim' function behavior in Photoshop 5.0 it appears that the
                // function returns true if the first parameter evaluates to true and the second parameter evaluates to false.

                BooleanConstantExpression first = Visit(args[0]) as BooleanConstantExpression;
                BooleanConstantExpression second = Visit(args[1]) as BooleanConstantExpression;

                if (first != null && second != null)
                {
                    result = new BooleanConstantExpression(first.Value && !second.Value);
                }
            }

            return result;
        }

        Expression IExpressionVisitor.VisitLogical(LogicalExpression node)
        {
            BooleanConstantExpression left = Visit(node.Left) as BooleanConstantExpression;
            if (left == null)
            {
                return BooleanConstantExpression.False;
            }

            switch (node.NodeType)
            {
                case ExpressionType.AndAlso:
                    if (!left.Value)
                    {
                        return left;
                    }
                    break;
                case ExpressionType.OrElse:
                    if (left.Value)
                    {
                        return left;
                    }
                    break;
                default:
                    throw new EnableInfoException(string.Format(CultureInfo.InvariantCulture, "{0} is not a supported logical expression.", node.NodeType));
            }

            BooleanConstantExpression right = Visit(node.Right) as BooleanConstantExpression;

            return right ?? BooleanConstantExpression.False;
        }

        Expression IExpressionVisitor.VisitParameter(ParameterExpression node)
        {
            return variables.GetValue(node.Name);
        }

        [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "ConstantExpression")]
        Expression IExpressionVisitor.VisitUnary(UnaryExpression node)
        {
            ConstantExpression expression = Visit(node.Expression) as ConstantExpression;
            if (expression == null)
            {
                throw new EnableInfoException(string.Format(CultureInfo.InvariantCulture,
                                                            "The {0} operand must be a ConstantExpression, actual type: {1}",
                                                            node.NodeType, node.Expression?.GetType().ToString() ?? "null"));
            }
            if (expression.ConstantType == ConstantType.UndefinedVariable)
            {
                return expression;
            }

            if (node.NodeType == ExpressionType.Not)
            {
                if (expression.ConstantType != ConstantType.Boolean)
                {
                    throw new EnableInfoException(string.Format(CultureInfo.InvariantCulture,
                                                                "The {0} operand must be a Boolean constant, actual value: {1}",
                                                                expression.NodeType, expression.ConstantType));
                }
                BooleanConstantExpression booleanConstant = (BooleanConstantExpression)expression;

                return new BooleanConstantExpression(!booleanConstant.Value);
            }
            else if (node.NodeType == ExpressionType.Negate || node.NodeType == ExpressionType.UnaryPlus)
            {
                if (expression.ConstantType != ConstantType.Integer)
                {
                    throw new EnableInfoException(string.Format(CultureInfo.InvariantCulture,
                                                                 "The {0} operand must be an Integer constant, actual value: {1}",
                                                                 expression.NodeType, expression.ConstantType));
                }
                IntegerConstantExpression integerConstant = (IntegerConstantExpression)expression;

                switch (node.NodeType)
                {
                    case ExpressionType.Negate:
                        return new IntegerConstantExpression(-integerConstant.Value);
                    case ExpressionType.UnaryPlus:
                        return new IntegerConstantExpression(+integerConstant.Value);
                    default:
                        throw new EnableInfoException(string.Format(CultureInfo.InvariantCulture, "{0} is not a supported unary operator.", expression.NodeType));
                }
            }
            else
            {
                throw new EnableInfoException(string.Format(CultureInfo.InvariantCulture, "{0} is not a supported unary operator.", expression.NodeType));
            }
        }
    }
}
