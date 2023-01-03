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
///
using System;
using System.Collections.ObjectModel;
using System.Text;

namespace PSFilterPdn.EnableInfo
{
    internal sealed class FunctionCallExpression : Expression
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FunctionCallExpression"/> class.
        /// </summary>
        /// <param name="name">The function name.</param>
        /// <param name="arguments">The function arguments.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="name"/> is null.
        /// </exception>
        public FunctionCallExpression(string name, ReadOnlyCollection<Expression> arguments)
        {
            if (name == null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            Name = name;
            Arguments = arguments;
        }

        public string Name { get; }

        public ReadOnlyCollection<Expression> Arguments { get; }

        public override ExpressionType NodeType => ExpressionType.FunctionCall;

        public override Expression Accept(IExpressionVisitor visitor)
        {
            return visitor.VisitFunctionCall(this);
        }

        public override string ToString()
        {
            StringBuilder sb = new(Name, 256);

            sb.Append("(");

            ReadOnlyCollection<Expression> args = Arguments;

            if (args != null && args.Count > 0)
            {
                int lastArg = args.Count - 1;

                for (int i = 0; i < args.Count; i++)
                {
                    sb.Append(args[i].ToString());
                    if (i < lastArg)
                    {
                        sb.Append(", ");
                    }
                }
            }

            sb.Append(")");

            return sb.ToString();
        }
    }
}
