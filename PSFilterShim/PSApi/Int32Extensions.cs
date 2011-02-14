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
    }
}
