﻿using System;
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
                i = 0;
            }

            if (i > 255)
            {
                i = 255;
            }

            return (byte)i;
        }
    } 
#endif
}
