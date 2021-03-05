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

using System.Collections.ObjectModel;

namespace PSFilterPdn.EnableInfo
{
    internal abstract class Expression
    {
        public abstract ExpressionType NodeType { get; }

        /// <summary>
        /// Creates a <see cref="BinaryExpression"/> that represents an addition operation.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>
        /// A <see cref="BinaryExpression"/> that represents an addition operation.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="left"/> is null.
        /// or
        /// <paramref name="right"/> is null.
        /// </exception>
        public static BinaryExpression Add(Expression left, Expression right)
        {
            return new BinaryExpression(left, ExpressionType.Add, right);
        }

        /// <summary>
        /// Creates a <see cref="LogicalExpression"/> that represents a conditional AND operation.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>
        /// A <see cref="LogicalExpression"/> that represents a conditional AND operation.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="left"/> is null.
        /// or
        /// <paramref name="right"/> is null.
        /// </exception>
        public static LogicalExpression AndAlso(Expression left, Expression right)
        {
            return new LogicalExpression(left, ExpressionType.AndAlso, right);
        }

        /// <summary>
        /// Creates a <see cref="FunctionCallExpression"/> that represents a function call.
        /// </summary>
        /// <param name="name">The function name.</param>
        /// <param name="arguments">The function arguments.</param>
        /// <returns>
        /// A <see cref="FunctionCallExpression"/> that represents a function call.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="name"/> is null.
        /// </exception>
        public static FunctionCallExpression Call(string name, ReadOnlyCollection<Expression> arguments)
        {
            return new FunctionCallExpression(name, arguments);
        }

        /// <summary>
        /// Creates a <see cref="ConstantExpression"/> that represents a Boolean constant.
        /// </summary>
        /// <param name="value">The Boolean value.</param>
        /// <returns>
        /// A <see cref="ConstantExpression"/> that represents a Boolean constant.
        /// </returns>
        public static ConstantExpression Constant(bool value)
        {
            return new BooleanConstantExpression(value);
        }

        /// <summary>
        /// Creates a <see cref="ConstantExpression"/> that represents an Integer constant.
        /// </summary>
        /// <param name="value">The Integer value.</param>
        /// <returns>
        /// A <see cref="ConstantExpression"/> that represents a Integer constant.
        /// </returns>
        public static ConstantExpression Constant(int value)
        {
            return new IntegerConstantExpression(value);
        }

        /// <summary>
        /// Creates a <see cref="ConstantExpression"/> that represents a String constant.
        /// </summary>
        /// <param name="value">The String value.</param>
        /// <returns>
        /// A <see cref="ConstantExpression"/> that represents a String constant.
        /// </returns>
        public static ConstantExpression Constant(string value)
        {
            return new StringConstantExpression(value);
        }

        /// <summary>
        /// Creates a <see cref="BinaryExpression"/> that represents a division operation.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>
        /// A <see cref="BinaryExpression"/> that represents a division operation.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="left"/> is null.
        /// or
        /// <paramref name="right"/> is null.
        /// </exception>
        public static BinaryExpression Divide(Expression left, Expression right)
        {
            return new BinaryExpression(left, ExpressionType.Divide, right);
        }

        /// <summary>
        /// Creates a <see cref="BinaryExpression"/> that represents an equality comparison.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>
        /// A <see cref="BinaryExpression"/> that represents an equality comparison.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="left"/> is null.
        /// or
        /// <paramref name="right"/> is null.
        /// </exception>
        public static BinaryExpression Equal(Expression left, Expression right)
        {
            return new BinaryExpression(left, ExpressionType.Equal, right);
        }

        /// <summary>
        /// Creates a <see cref="BinaryExpression"/> that represents a "greater than" numeric comparison.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>
        /// A <see cref="BinaryExpression"/> that represents a "greater than" numeric comparison.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="left"/> is null.
        /// or
        /// <paramref name="right"/> is null.
        /// </exception>
        public static BinaryExpression GreaterThan(Expression left, Expression right)
        {
            return new BinaryExpression(left, ExpressionType.GreaterThan, right);
        }

        /// <summary>
        /// Creates a <see cref="BinaryExpression"/> that represents a "greater than or equal" numeric comparison.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>
        /// A <see cref="BinaryExpression"/> that represents a "greater than or equal" numeric comparison.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="left"/> is null.
        /// or
        /// <paramref name="right"/> is null.
        /// </exception>
        public static BinaryExpression GreaterThanOrEqual(Expression left, Expression right)
        {
            return new BinaryExpression(left, ExpressionType.GreaterThanOrEqual, right);
        }

        /// <summary>
        /// Creates a <see cref="BinaryExpression"/> that represents a "less than" numeric comparison.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>
        /// A <see cref="BinaryExpression"/> that represents an "less than" numeric comparison.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="left"/> is null.
        /// or
        /// <paramref name="right"/> is null.
        /// </exception>
        public static BinaryExpression LessThan(Expression left, Expression right)
        {
            return new BinaryExpression(left, ExpressionType.LessThan, right);
        }

        /// <summary>
        /// Creates a <see cref="BinaryExpression"/> that represents a "less than or equal" numeric comparison.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>
        /// A <see cref="BinaryExpression"/> that represents a "less than or equal" numeric comparison.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="left"/> is null.
        /// or
        /// <paramref name="right"/> is null.
        /// </exception>
        public static BinaryExpression LessThanOrEqual(Expression left, Expression right)
        {
            return new BinaryExpression(left, ExpressionType.LessThanOrEqual, right);
        }

        /// <summary>
        /// Creates a <see cref="BinaryExpression"/> that represents a multiplication operation.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>
        /// A <see cref="BinaryExpression"/> that represents a multiplication operation.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="left"/> is null.
        /// or
        /// <paramref name="right"/> is null.
        /// </exception>
        public static BinaryExpression Multiply(Expression left, Expression right)
        {
            return new BinaryExpression(left, ExpressionType.Multiply, right);
        }

        /// <summary>
        /// Creates a <see cref="UnaryExpression"/> that represents an numeric negation operation.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns>A <see cref="UnaryExpression"/> that represents an numeric negation operation.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="expression"/> is null.
        /// </exception>
        public static UnaryExpression Negate(Expression expression)
        {
            return new UnaryExpression(expression, ExpressionType.Negate);
        }

        /// <summary>
        /// Creates a <see cref="UnaryExpression"/> that represents a logical negation operation.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns>A <see cref="UnaryExpression"/> that represents a logical negation operation.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="expression"/> is null.
        /// </exception>
        public static UnaryExpression Not(Expression expression)
        {
            return new UnaryExpression(expression, ExpressionType.Not);
        }

        /// <summary>
        /// Creates a <see cref="BinaryExpression"/> that represents an inequality comparison.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>
        /// A <see cref="BinaryExpression"/> that represents an inequality comparison.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="left"/> is null.
        /// or
        /// <paramref name="right"/> is null.
        /// </exception>
        public static BinaryExpression NotEqual(Expression left, Expression right)
        {
            return new BinaryExpression(left, ExpressionType.NotEqual, right);
        }

        /// <summary>
        /// Creates a <see cref="LogicalExpression"/> that represents a conditional OR operation.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>
        /// A <see cref="LogicalExpression"/> that represents a conditional OR operation.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="left"/> is null.
        /// or
        /// <paramref name="right"/> is null.
        /// </exception>
        public static LogicalExpression OrElse(Expression left, Expression right)
        {
            return new LogicalExpression(left, ExpressionType.OrElse, right);
        }

        /// <summary>
        /// Creates a <see cref="BinaryExpression"/> that represents a subtraction operation.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="right">The right operand.</param>
        /// <returns>
        /// A <see cref="BinaryExpression"/> that represents a subtraction operation.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="left"/> is null.
        /// or
        /// <paramref name="right"/> is null.
        /// </exception>
        public static BinaryExpression Subtract(Expression left, Expression right)
        {
            return new BinaryExpression(left, ExpressionType.Subtract, right);
        }

        /// <summary>
        /// Creates a <see cref="UnaryExpression"/> that represents a unary plus operation.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <returns>A <see cref="UnaryExpression"/> that represents a unary plus operation.</returns>
        /// <exception cref="System.ArgumentNullException">
        /// <paramref name="expression"/> is null.
        /// </exception>
        public static UnaryExpression UnaryPlus(Expression expression)
        {
            return new UnaryExpression(expression, ExpressionType.UnaryPlus);
        }

        /// <summary>
        /// Creates a <see cref="ParameterExpression"/> that represents a variable.
        /// </summary>
        /// <param name="name">The variable name.</param>
        /// <returns>A <see cref="ParameterExpression"/> that represents a variable.</returns>
        public static ParameterExpression Variable(string name)
        {
            return new ParameterExpression(name);
        }

        /// <summary>
        /// Dispatches to the specific visit method for this node type.
        /// </summary>
        /// <param name="visitor">The visitor to visit this node with.</param>
        /// <returns>The result of visiting this node.</returns>
        public abstract Expression Accept(IExpressionVisitor visitor);

        public abstract override string ToString();
    }
}
