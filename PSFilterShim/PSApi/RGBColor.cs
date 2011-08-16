/* Adapted from PITypes.h
 * Copyright (c) 1992-6, Adobe Systems Incorporated.
 * All rights reserved.
*/
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct RGBColor
    {
        /// WORD->unsigned short
        public ushort red;
        /// WORD->unsigned short
        public ushort green;
        /// WORD->unsigned short
        public ushort blue;
    }

}
