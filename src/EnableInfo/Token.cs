﻿/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
//   Copyright (C) 2010-2024 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System.Globalization;

namespace PSFilterPdn.EnableInfo
{
    internal sealed class Token
    {
        public Token(TokenType type) : this(type, string.Empty)
        {
        }

        public Token(TokenType type, string value)
        {
            Type = type;
            Value = value;
        }

        public TokenType Type { get; }

        public string Value { get; }

        public override string ToString()
        {
            if (!string.IsNullOrEmpty(Value))
            {
                return string.Format(CultureInfo.InvariantCulture, "Type: {0}, Value: {1}", Type, Value);
            }
            else
            {
                return string.Format(CultureInfo.InvariantCulture, "Type: {0}", Type);
            }
        }
    }
}
