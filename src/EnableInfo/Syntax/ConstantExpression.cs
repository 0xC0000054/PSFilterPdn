/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
//   Copyright (C) 2010-2023 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System.Diagnostics;

namespace PSFilterPdn.EnableInfo
{
    internal enum ConstantType
    {
        Boolean,
        Integer,
        String,
        UndefinedVariable
    }

    internal abstract class ConstantExpression : Expression
    {
        public abstract ConstantType ConstantType { get; }

        public sealed override ExpressionType NodeType => ExpressionType.Constant;

        public sealed override Expression Accept(IExpressionVisitor visitor)
        {
            return visitor.VisitConstant(this);
        }
    }

    [DebuggerDisplay("{Value, nq}")]
    internal sealed class BooleanConstantExpression : ConstantExpression
    {
        internal static readonly BooleanConstantExpression True = new(true);
        internal static readonly BooleanConstantExpression False = new(false);

        /// <summary>
        /// Initializes a new instance of the <see cref="BooleanConstantExpression"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        public BooleanConstantExpression(bool value)
        {
            Value = value;
        }

        public override ConstantType ConstantType => ConstantType.Boolean;

        public bool Value { get; }

        public override string ToString()
        {
            return Value.ToString();
        }
    }

    [DebuggerDisplay("{Value, nq}")]
    internal sealed class IntegerConstantExpression : ConstantExpression
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IntegerConstantExpression"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        public IntegerConstantExpression(int value)
        {
            Value = value;
        }

        public override ConstantType ConstantType => ConstantType.Integer;

        public int Value { get; }

        public override string ToString()
        {
            return Value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    [DebuggerDisplay("{Value, nq}")]
    internal sealed class StringConstantExpression : ConstantExpression
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StringConstantExpression"/> class.
        /// </summary>
        /// <param name="value">The value.</param>
        public StringConstantExpression(string value)
        {
            Value = value ?? string.Empty;
        }

        public override ConstantType ConstantType => ConstantType.String;

        public string Value { get; }

        public override string ToString()
        {
            return Value;
        }
    }

    [DebuggerDisplay("{Name, nq}")]
    internal sealed class UndefinedVariableConstantExpression : ConstantExpression
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UndefinedVariableConstantExpression"/> class.
        /// </summary>
        /// <param name="name">The name.</param>
        public UndefinedVariableConstantExpression(string name)
        {
            Name = name ?? string.Empty;
        }

        public override ConstantType ConstantType => ConstantType.UndefinedVariable;

        public string Name { get; }

        public override string ToString()
        {
            return Name;
        }
    }
}
