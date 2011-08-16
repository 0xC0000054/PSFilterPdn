/* Adapted from PITypes.h
 * Copyright (c) 1992-6, Adobe Systems Incorporated.
 * All rights reserved.
*/
using System.Runtime.InteropServices;
namespace PSFilterLoad.PSApi
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct VRect
    {

        /// int32->int
        public int top;

        /// int32->int
        public int left;

        /// int32->int
        public int bottom;

        /// int32->int
        public int right;

#if DEBUG
        public override string ToString()
        {
            return ("Top=" + this.top.ToString() + ",Bottom=" + this.bottom.ToString() + ",Left=" + this.left.ToString() + ",Right=" + this.right.ToString());
        }
#endif
    }
}