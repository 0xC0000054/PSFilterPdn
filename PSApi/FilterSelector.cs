/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2018 Nicholas Hayes
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
    internal enum FilterSelector : short
    {
        About = 0,
        Parameters = 1,
        Prepare = 2,
        Start = 3,
        Continue = 4,
        Finish = 5
    }
}
