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

using System;

namespace PSFilterLoad.PSApi
{
    internal sealed class UnsupportedPICASuiteVersionException : NotSupportedException
    {
        public UnsupportedPICASuiteVersionException(string suiteName, int suiteVersion)
            : base($"PICA suite not supported: '{suiteName}', version {suiteVersion}.")
        {
        }
    }
}
