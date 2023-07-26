﻿/////////////////////////////////////////////////////////////////////////////////
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

namespace PSFilterPdn
{
    internal sealed class PSFilterShimErrorInfo
    {
        public PSFilterShimErrorInfo(string message) : this(message, string.Empty)
        {
        }

        public PSFilterShimErrorInfo(string message, string details)
        {
            Message = message ?? string.Empty;
            Details = details ?? string.Empty;
        }

        public string Message { get; }

        public string Details { get; }
    }
}