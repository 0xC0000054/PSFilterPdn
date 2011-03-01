using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PSFilterLoad.PSApi
{
    static class Int32Extensions
    {
        public static int Clamp(this int i, int min, int max)
        {
            if (i < min)
            {
                i = min;
            }
             
            if (i > max)
	        {
		       i = max;
	        }

            return i;
        }
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
}
