/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2023 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;

namespace PSFilterPdn.EnableInfo
{
    internal sealed class UnaryExpression : Expression
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UnaryExpression"/> class.
        /// </summary>
        /// <param name="expression">The expression.</param>
        /// <param name="op">The unary operand.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="expression"/> is null.
        /// </exception>
        public UnaryExpression(Expression expression, ExpressionType op)
        {
            ArgumentNullException.ThrowIfNull(expression, nameof(expression));

            Expression = expression;
            NodeType = op;
        }

        public Expression Expression { get; }

        public override ExpressionType NodeType { get; }

        public override Expression Accept(IExpressionVisitor visitor)
        {
            return visitor.VisitUnary(this);
        }

        public override string ToString()
        {
            switch (NodeType)
            {
                case ExpressionType.Negate:
                    return "-" + Expression;
                case ExpressionType.Not:
                    return "!" + Expression;
                case ExpressionType.UnaryPlus:
                    return "+" + Expression;
                default:
                    throw new InvalidOperationException(string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0} is not a supported unary operator.", NodeType));
            }
        }
    }
}
