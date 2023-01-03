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

namespace PSFilterLoad.PSApi.Diagnostics
{
    // Adapted from https://stackoverflow.com/a/26784846
    /// <summary>
    /// This type is used as a buffer between the end of a the actual parameter list
    /// and the start of the optional parameter list.
    /// This prevents the compiler from selecting the wrong overload.
    /// </summary>
    internal readonly struct OptionalParameterGuard
    {
    }
}
