using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PSFilterLoad.PSApi
{
    internal delegate short colorServicesChooseColor(IntPtr colors, short sourceSpace, short resultSpace, IntPtr pickerPrompt);
    internal delegate short colorServicesConvertColor(short sourceSpace, short resultSpace, IntPtr colors);
    internal delegate short colorServicesGetSpecialColor(IntPtr colors, short sourceSpace, short resultSpace, int specialColor);
    internal delegate short colorServicesSelectPoint(IntPtr colors, short sourceSpace, short resultSpace, IntPtr globalSamplePoint);

}
