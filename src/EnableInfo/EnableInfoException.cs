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

namespace PSFilterPdn.EnableInfo
{
    [Serializable]
    internal sealed class EnableInfoException : Exception
    {
        public EnableInfoException()
        {
        }

        public EnableInfoException(string message) : base(message)
        {
        }

        public EnableInfoException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
