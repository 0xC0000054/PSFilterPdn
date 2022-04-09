/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2022 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

/* Adapted from PIGeneral.h
 * Copyright (c) 1992-1998, Adobe Systems Incorporated.
 * All rights reserved.
*/

namespace PSFilterLoad.PSApi.Loader
{
    // Disable CS0649, Field 'field' is never assigned to, and will always have its default value 'value'
#pragma warning disable 0649

    internal struct PITerminology
    {
        public int version;
        public uint classID;
        public uint eventID;
        public short terminologyID;

#if DEBUG
        public const int SizeOf = 14;
#endif
    }
#pragma warning restore 0649

}
