using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PSFilterPdn
{
    static class BooleanExtensions
    {
        public static byte ToByte(this bool value)
        {
            if (value)
            {
                return 1;
            }
            return 0;
        }
    }
}
