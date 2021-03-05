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

namespace PSFilterPdn.EnableInfo
{
    internal interface IExpressionVisitor
    {
        Expression VisitBinary(BinaryExpression node);
        Expression VisitConstant(ConstantExpression node);
        Expression VisitFunctionCall(FunctionCallExpression node);
        Expression VisitLogical(LogicalExpression node);
        Expression VisitParameter(ParameterExpression node);
        Expression VisitUnary(UnaryExpression node);
    }
}
