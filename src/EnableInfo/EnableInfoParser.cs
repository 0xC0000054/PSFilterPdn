/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2022 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;

namespace PSFilterPdn.EnableInfo
{
    internal sealed class EnableInfoParser
    {
        private readonly ReadOnlyCollection<Token> tokens;
        private int index;

        private static readonly Token EndOfFile = new Token(TokenType.EndOfFile);

        /// <summary>
        /// Initializes a new instance of the <see cref="EnableInfoParser"/> class.
        /// </summary>
        /// <param name="tokens">The tokens.</param>
        /// <exception cref="ArgumentNullException"><paramref name="tokens"/> is null.</exception>
        private EnableInfoParser(ReadOnlyCollection<Token> tokens)
        {
            if (tokens == null)
            {
                throw new ArgumentNullException(nameof(tokens));
            }

            this.tokens = tokens;
            index = 0;
        }

        private bool IsAtEnd => index == tokens.Count;

        /// <summary>
        /// Parses the specified enable information string.
        /// </summary>
        /// <param name="enableInfo">The enable information string.</param>
        /// <returns>
        /// An <see cref="Expression"/> representing the parsed enable information.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="enableInfo"/> is null.</exception>
        /// <exception cref="EnableInfoException">The enable information string is not valid.</exception>
        public static Expression Parse(string enableInfo)
        {
            if (enableInfo == null)
            {
                throw new ArgumentNullException(nameof(enableInfo));
            }

            ReadOnlyCollection<Token> tokens = new EnableInfoLexer(enableInfo).GetTokens();

            if (tokens.Count == 0)
            {
                throw new EnableInfoException("The enable information string does not contain any tokens.");
            }

            return new EnableInfoParser(tokens).Parse();
        }

        private Expression Parse()
        {
            return ParseBooleanExpression();
        }

        private Token Advance()
        {
            Token current;

            if (IsAtEnd)
            {
                current = EndOfFile;
            }
            else
            {
                current = tokens[index];
                index++;
            }

            return current;
        }

        private Token Peek()
        {
            return IsAtEnd ? EndOfFile : tokens[index];
        }

        private Expression ParseBooleanExpression()
        {
            Expression left = ParseConjunction();

            Token op = Peek();

            while (op.Type == TokenType.ConditionalOr)
            {
                Advance();

                Expression right = ParseConjunction();

                left = Expression.OrElse(left, right);

                op = Peek();
            }

            return left;
        }

        private Expression ParseConjunction()
        {
            Expression left = ParseRelation();

            Token op = Peek();

            while (op.Type == TokenType.ConditionalAnd)
            {
                Advance();

                Expression right = ParseRelation();

                left = Expression.AndAlso(left, right);

                op = Peek();
            }

            return left;
        }

        private Expression ParseRelation()
        {
            Expression left = ParseEquality();

            Token op = Peek();

            if (op.Type == TokenType.GreaterThan ||
                op.Type == TokenType.GreaterThanOrEqual ||
                op.Type == TokenType.LessThan ||
                op.Type == TokenType.LessThanOrEqual)
            {
                Advance();

                Expression right = ParseEquality();

                switch (op.Type)
                {
                    case TokenType.GreaterThan:
                        left = Expression.GreaterThan(left, right);
                        break;
                    case TokenType.GreaterThanOrEqual:
                        left = Expression.GreaterThanOrEqual(left, right);
                        break;
                    case TokenType.LessThan:
                        left = Expression.LessThan(left, right);
                        break;
                    case TokenType.LessThanOrEqual:
                        left = Expression.LessThanOrEqual(left, right);
                        break;
                }
            }

            return left;
        }

        private Expression ParseEquality()
        {
            Expression left = ParseSimpleExpression();

            Token op = Peek();

            if (op.Type == TokenType.Equal || op.Type == TokenType.NotEqual)
            {
                Advance();

                Expression right = ParseSimpleExpression();

                switch (op.Type)
                {
                    case TokenType.Equal:
                        left = Expression.Equal(left, right);
                        break;
                    case TokenType.NotEqual:
                        left = Expression.NotEqual(left, right);
                        break;
                }
            }

            return left;
        }

        private Expression ParseSimpleExpression()
        {
            Expression left = ParseTerm();

            Token op = Peek();

            while (op.Type == TokenType.Plus || op.Type == TokenType.Minus)
            {
                Advance();

                Expression right = ParseTerm();

                switch (op.Type)
                {
                    case TokenType.Plus:
                        left = Expression.Add(left, right);
                        break;
                    case TokenType.Minus:
                        left = Expression.Subtract(left, right);
                        break;
                }

                op = Peek();
            }

            return left;
        }

        private Expression ParseTerm()
        {
            Expression left = ParseFactor();

            Token op = Peek();

            while (op.Type == TokenType.Multiply || op.Type == TokenType.Divide)
            {
                Advance();

                Expression right = ParseFactor();

                switch (op.Type)
                {
                    case TokenType.Multiply:
                        left = Expression.Multiply(left, right);
                        break;
                    case TokenType.Divide:
                        left = Expression.Divide(left, right);
                        break;
                }

                op = Peek();
            }

            return left;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "booleanExpression")]
        private Expression ParseFactor()
        {
            Token token = Advance();

            if (token.Type == TokenType.IntegerConstant)
            {
                if (!int.TryParse(token.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
                {
                    throw new EnableInfoException("Integer constant must be in the range of [-2147483648, 2147483647], actual value: " + token.Value);
                }

                return Expression.Constant(value);
            }
            else if (token.Type == TokenType.BooleanConstant)
            {
                if (!bool.TryParse(token.Value, out bool value))
                {
                    throw new EnableInfoException("Boolean constant must be 'true' or 'false', actual value: " + token.Value);
                }

                return Expression.Constant(value);
            }
            else if (token.Type == TokenType.StringConstant)
            {
                return Expression.Constant(token.Value);
            }
            else if (token.Type == TokenType.Identifier)
            {
                return Expression.Variable(token.Value);
            }
            else if (token.Type == TokenType.FunctionCall)
            {
                return ParseFunctionCall(token.Value);
            }
            else if (token.Type == TokenType.LeftParentheses)
            {
                if (Peek().Type == TokenType.RightParentheses)
                {
                    throw new EnableInfoException("Empty () statement in a ( <booleanExpression> ) block.");
                }

                Expression expression = ParseBooleanExpression();

                Consume(TokenType.RightParentheses, "Missing ')' after a ( <booleanExpression> ) block.");

                return expression;
            }
            else
            {
                switch (token.Type)
                {
                    case TokenType.Plus:
                        return Expression.UnaryPlus(ParseFactor());
                    case TokenType.Minus:
                        return Expression.Negate(ParseFactor());
                    case TokenType.Not:
                        return Expression.Not(ParseFactor());
                    case TokenType.EndOfFile:
                        throw new EnableInfoException("Expected more tokens.");
                    default:
                        throw new EnableInfoException("Unsupported token type: " + token.Type);
                }
            }
        }

        private FunctionCallExpression ParseFunctionCall(string functionName)
        {
            Consume(TokenType.LeftParentheses, "Missing '(' in a function call expression.");

            List<Expression> args = new List<Expression>();

            if (Peek().Type != TokenType.RightParentheses)
            {
                do
                {
                    args.Add(ParseSimpleExpression());

                } while (IsMatch(TokenType.ArgumentSeparator));
            }
            Consume(TokenType.RightParentheses, "Missing ')' in a function call expression.");

            return Expression.Call(functionName, args.AsReadOnly());
        }

        private Token Consume(TokenType type, string missingTokenError)
        {
            if (Peek().Type == type)
            {
                return Advance();
            }
            else
            {
                throw new EnableInfoException(missingTokenError);
            }
        }

        private bool IsMatch(TokenType type)
        {
            if (Peek().Type == type)
            {
                Advance();
                return true;
            }

            return false;
        }
    }
}
