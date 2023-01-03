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

/* Adapted from PIGeneral.h
 * Copyright (c) 1992-1998, Adobe Systems Incorporated.
 * All rights reserved.
*/

namespace PSFilterLoad.PSApi.Loader
{
    // Disable CS0649, Field 'field' is never assigned to, and will always have its default value 'value'
#pragma warning disable 0649

    internal unsafe struct PIProperty
    {
        public uint vendorID;  /* Vendor specific identifier. */
        public uint propertyKey;        /* Identification key for this property type. */
        public int propertyID;      /* Index within this property type. Must be unique for properties of a given type in a PiPL. */
        public int propertyLength;  /* Length of following data array. Will be rounded to a multiple of 4. */
        public fixed byte propertyData[1];
    }
#pragma warning restore 0649

}
