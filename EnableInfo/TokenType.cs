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

namespace PSFilterPdn.EnableInfo
{
    internal enum TokenType
    {
        EndOfFile,
        LeftParentheses,
        RightParentheses,
        ArgumentSeparator,
        FunctionCall,
        Identifier,
        BooleanConstant,
        IntegerConstant,
        StringConstant,
        // Logical operators
        ConditionalOr,
        ConditionalAnd,
        // Equality operators
        Equal,
        NotEqual,
        // Relational operators
        GreaterThan,
        GreaterThanOrEqual,
        LessThan,
        LessThanOrEqual,
        // Multiplicative operators
        Multiply,
        Divide,
        // Additive and unary operators
        Plus,
        Minus,
        Not
    }
}
