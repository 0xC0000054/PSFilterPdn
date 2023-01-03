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

using System;
using System.Runtime.Serialization;

namespace PSFilterPdn.EnableInfo
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1064:ExceptionsShouldBePublic")]
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

        private EnableInfoException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
