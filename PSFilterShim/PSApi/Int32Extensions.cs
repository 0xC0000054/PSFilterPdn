/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2017 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PSFilterLoad.PSApi
{
#if USEMATTING
    static class Int32Extensions
    {
        public static byte ClampToByte(this int i)
        {
            if (i < 0)
            {
                return 0;
            }

            if (i > 255)
            {
                return 255;
            }

            return (byte)i;
        }
    } 
#endif
}
