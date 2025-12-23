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

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace PSFilterPdn.EnableInfo
{
    internal sealed class EnableInfoLexer
    {
        private readonly string source;
        private readonly int length;
        private int index;
        private ReadOnlyCollection<Token>? readOnlyTokens;

        /// <summary>
        /// Initializes a new instance of the <see cref="EnableInfoLexer"/> class.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <exception cref="ArgumentNullException"><paramref name="input"/> is null.</exception>
        public EnableInfoLexer(string input)
        {
            ArgumentNullException.ThrowIfNull(input, nameof(input));

            source = input;
            index = 0;
            length = input.Length;
        }

        /// <summary>
        /// Gets the tokens in the enable information string.
        /// </summary>
        /// <returns>The tokens in the enable information string.</returns>
        /// <exception cref="EnableInfoException">The enable information string is not valid.</exception>
        public ReadOnlyCollection<Token> GetTokens()
        {
            readOnlyTokens ??= new ReadOnlyCollection<Token>(ScanTokens());

            return readOnlyTokens;
        }

        private bool IsAtEnd => index == length;

        private char Advance()
        {
            char current;

            if (IsAtEnd)
            {
                current = '\0';
            }
            else
            {
                current = source[index];
                index++;
            }

            return current;
        }

        private char Peek()
        {
            return IsAtEnd ? '\0' : source[index];
        }

        private string ReadWhile(Predicate<char> predicate)
        {
            // Subtract one from the index to include the first character.
            int startIndex = index - 1;

            while (index < length && predicate(source[index]))
            {
                index++;
            }

            return source.Substring(startIndex, index - startIndex);
        }

        private bool NextCharMatches(char value)
        {
            if (Peek() == value)
            {
                Advance();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Scans the tokens.
        /// </summary>
        /// <returns>A list of tokens.</returns>
        /// <exception cref="EnableInfoException">
        /// The enable information string is not valid.
        /// </exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        private IList<Token> ScanTokens()
        {
            List<Token> tokens = [];
            index = 0;

            while (!IsAtEnd)
            {
                char c = Advance();

                if (char.IsWhiteSpace(c))
                {
                    continue;
                }

                switch (c)
                {
                    case '|':
                        if (NextCharMatches('|'))
                        {
                            tokens.Add(new Token(TokenType.ConditionalOr));
                        }
                        else
                        {
                            throw new EnableInfoException("Only one | found in conditional OR comparison.");
                        }
                        break;
                    case '&':
                        if (NextCharMatches('&'))
                        {
                            tokens.Add(new Token(TokenType.ConditionalAnd));
                        }
                        else
                        {
                            throw new EnableInfoException("Only one & found in conditional AND comparison.");
                        }
                        break;
                    case '>':
                        if (NextCharMatches('='))
                        {
                            tokens.Add(new Token(TokenType.GreaterThanOrEqual));
                        }
                        else
                        {
                            tokens.Add(new Token(TokenType.GreaterThan));
                        }
                        break;
                    case '<':
                        if (NextCharMatches('='))
                        {
                            tokens.Add(new Token(TokenType.LessThanOrEqual));
                        }
                        else
                        {
                            tokens.Add(new Token(TokenType.LessThan));
                        }
                        break;
                    case '!':
                        if (NextCharMatches('='))
                        {
                            tokens.Add(new Token(TokenType.NotEqual));
                        }
                        else
                        {
                            tokens.Add(new Token(TokenType.Not));
                        }
                        break;
                    case '=':
                        if (NextCharMatches('='))
                        {
                            tokens.Add(new Token(TokenType.Equal));
                        }
                        else
                        {
                            throw new EnableInfoException("Only one = found in equality comparison.");
                        }
                        break;
                    case '+':
                        tokens.Add(new Token(TokenType.Plus));
                        break;
                    case '-':
                        tokens.Add(new Token(TokenType.Minus));
                        break;
                    case '*':
                        tokens.Add(new Token(TokenType.Multiply));
                        break;
                    case '/':
                        tokens.Add(new Token(TokenType.Divide));
                        break;
                    case '(':
                        tokens.Add(new Token(TokenType.LeftParentheses));
                        break;
                    case ')':
                        tokens.Add(new Token(TokenType.RightParentheses));
                        break;
                    case ',':
                        tokens.Add(new Token(TokenType.ArgumentSeparator));
                        break;
                    default:
                        if (EnableInfoSyntax.IsNumber(c))
                        {
                            string value = ReadWhile(EnableInfoSyntax.IsNumber);

                            tokens.Add(new Token(TokenType.IntegerConstant, value));
                        }
                        else if (EnableInfoSyntax.IsStartOfIdentifier(c))
                        {
                            string identifier = ReadWhile(EnableInfoSyntax.IsPartOfIdentifier);

                            if (EnableInfoSyntax.IsBooleanConstant(identifier))
                            {
                                tokens.Add(new Token(TokenType.BooleanConstant, identifier));
                            }
                            else if (EnableInfoSyntax.IsFunctionCall(identifier))
                            {
                                tokens.Add(new Token(TokenType.FunctionCall, identifier));
                            }
                            else if (EnableInfoSyntax.IsStringConstant(identifier))
                            {
                                tokens.Add(new Token(TokenType.StringConstant, identifier));
                            }
                            else
                            {
                                tokens.Add(new Token(TokenType.Identifier, identifier));
                            }
                        }
                        else
                        {
                            throw new EnableInfoException("Unexpected character: " + c);
                        }
                        break;
                }
            }

            return tokens;
        }

        private static class EnableInfoSyntax
        {
            private static readonly HashSet<string> stringConstants = new(StringComparer.OrdinalIgnoreCase)
            {
                "BitmapMode",
                "GrayScaleMode",
                "IndexedMode",
                "RGBMode",
                "CMYKMode",
                "HSLMode",
                "HSBMode",
                "MultichannelMode",
                "DuotoneMode",
                "LabMode",
                "Gray16Mode",
                "RGB48Mode",
                "Lab48Mode",
                "CMYK64Mode",
                "DeepMultichannelMode",
                "Duotone16Mode",
                "RGB96Mode",
                "Gray32Mode"
            };

            private static readonly HashSet<string> functions = new(StringComparer.OrdinalIgnoreCase)
            {
                "in",
                "min",
                "max",
                "dim"
            };

            public static bool IsBooleanConstant(string identifier)
            {
                return string.Equals(identifier, "true", StringComparison.OrdinalIgnoreCase) || string.Equals(identifier, "false", StringComparison.OrdinalIgnoreCase);
            }

            public static bool IsFunctionCall(string identifier)
            {
                return functions.Contains(identifier);
            }

            public static bool IsStringConstant(string identifier)
            {
                return stringConstants.Contains(identifier);
            }

            public static bool IsNumber(char token)
            {
                return char.IsDigit(token);
            }

            public static bool IsPartOfIdentifier(char token)
            {
                return char.IsLetterOrDigit(token) || token == '_';
            }

            public static bool IsStartOfIdentifier(char token)
            {
                return char.IsLetter(token) || token == '_';
            }
        }
    }
}
