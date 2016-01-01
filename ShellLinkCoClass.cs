/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// http://psfilterpdn.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2016 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Runtime.InteropServices;

namespace PSFilterPdn
{
    /// <summary>
    /// The ShellLink CoCreate class used by the framework for interop
    /// </summary>
    [ComImport(), Guid("00021401-0000-0000-C000-000000000046")]    
    class ShellLinkCoClass
    {
    }
}
