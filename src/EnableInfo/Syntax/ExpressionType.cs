/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
//   Copyright (C) 2010-2025 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

namespace PSFilterPdn.EnableInfo
{
    internal enum ExpressionType
    {
        /// <summary>
        /// An addition operation.
        /// </summary>
        Add,
        /// <summary>
        /// A short-circuiting conditional AND operation.
        /// </summary>
        AndAlso,
        /// <summary>
        /// A constant value.
        /// </summary>
        Constant,
        /// <summary>
        /// A division operation.
        /// </summary>
        Divide,
        /// <summary>
        /// An equality operation.
        /// </summary>
        Equal,
        /// <summary>
        /// A function call
        /// </summary>
        FunctionCall,
        /// <summary>
        /// A greater than operation.
        /// </summary>
        GreaterThan,
        /// <summary>
        /// A greater than or equal operation.
        /// </summary>
        GreaterThanOrEqual,
        /// <summary>
        /// A less than operation.
        /// </summary>
        LessThan,
        /// <summary>
        /// A less than or equal operation.
        /// </summary>
        LessThanOrEqual,
        /// <summary>
        /// A multiplication operation.
        /// </summary>
        Multiply,
        /// <summary>
        /// An arithmetic negation operation.
        /// </summary>
        Negate,
        /// <summary>
        /// A logical negation operation.
        /// </summary>
        Not,
        /// <summary>
        /// An inequality operation.
        /// </summary>
        NotEqual,
        /// <summary>
        /// A short-circuiting conditional OR operation.
        /// </summary>
        OrElse,
        /// <summary>
        /// A variable that is defined in the expression
        /// </summary>
        Parameter,
        /// <summary>
        /// A subtraction operation.
        /// </summary>
        Subtract,
        /// <summary>
        /// A unary plus operation.
        /// </summary>
        UnaryPlus
    }
}
