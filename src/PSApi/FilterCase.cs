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

/* Adapted from PIFilter.h
 * Copyright (c) 1990-1991, Thomas Knoll.
 * Copyright (c) 1992-1998, Adobe Systems Incorporated.
 * All rights reserved.
*/

namespace PSFilterLoad.PSApi
{
    internal enum FilterCase : short
    {
        FlatImageNoSelection = 1,
        FlatImageWithSelection = 2,
        FloatingSelection = 3,
        EditableTransparencyNoSelection = 4,
        EditableTransparencyWithSelection = 5,
        ProtectedTransparencyNoSelection = 6,
        ProtectedTransparencyWithSelection = 7
    }
}