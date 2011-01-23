using System.Runtime.InteropServices;


namespace PSFilterLoad.PSApi
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    unsafe struct AboutRecord 
    {
        /// void*
        public System.IntPtr platformData;
    
        /// char[244]
        public fixed byte reserved[252];
    }

}
