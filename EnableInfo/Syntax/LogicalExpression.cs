/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2020 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Globalization;

namespace PSFilterPdn.EnableInfo
{
    internal sealed class LogicalExpression : Expression
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LogicalExpression"/> class.
        /// </summary>
        /// <param name="left">The left operand.</param>
        /// <param name="op">The logical operator.</param>
        /// <param name="right">The right operand.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="left"/> is null.
        /// or
        /// <paramref name="right"/> is null.
        /// </exception>
        public LogicalExpression(Expression left, ExpressionType op, Expression right)
        {
            if (left == null)
            {
                throw new ArgumentNullException(nameof(left));
            }
            if (right == null)
            {
                throw new ArgumentNullException(nameof(right));
            }

            Left = left;
            NodeType = op;
            Right = right;
        }

        public Expression Left { get; }

        public override ExpressionType NodeType { get; }

        public Expression Right { get; }

        public override Expression Accept(IExpressionVisitor visitor)
        {
            return visitor.VisitLogical(this);
        }

        public override string ToString()
        {
            switch (NodeType)
            {
                case ExpressionType.AndAlso:
                    return string.Format(CultureInfo.InvariantCulture, "{0} && {1}", Left, Right);
                case ExpressionType.OrElse:
                    return string.Format(CultureInfo.InvariantCulture, "{0} || {1}", Left, Right);
                default:
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "{0} is not a supported logical operator.", NodeType));
            }
        }
    }
}
