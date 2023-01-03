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

namespace PSFilterPdn.Interop
{
    internal static class NativeEnums
    {
#pragma warning disable RCS1135 // Declare enum member with zero value (when enum has FlagsAttribute).
#pragma warning disable RCS1154 // Sort enum members.
#pragma warning disable RCS1191 // Declare enum value as combination of names.

        [Flags]
        internal enum TCHITTESTFLAGS
        {
            TCHT_NOWHERE = 1,
            TCHT_ONITEMICON = 2,
            TCHT_ONITEMLABEL = 4,
            TCHT_ONITEM = TCHT_ONITEMICON | TCHT_ONITEMLABEL
        }

#pragma warning restore RCS1135 // Declare enum member with zero value (when enum has FlagsAttribute).
#pragma warning restore RCS1154 // Sort enum members.
#pragma warning restore RCS1191 // Declare enum value as combination of names.
    }
}

