namespace PSFilterLoad.PSApi
{


    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal struct PSPixelMask
    {

        /// PSPixelMask*
        public System.IntPtr next;

        /// void*
        public System.IntPtr maskData;

        /// int32->int
        public int rowBytes;

        /// int32->int
        public int colBytes;

        /// int32->int
        public int maskDescription;
    }

    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal struct PSPixelOverlay
    {

        /// PSPixelOverlay*
        public System.IntPtr next;

        /// void*
        public System.IntPtr data;

        /// int32->int
        public int rowBytes;

        /// int32->int
        public int colBytes;

        /// unsigned8->unsigned char
        public byte r;

        /// unsigned8->unsigned char
        public byte g;

        /// unsigned8->unsigned char
        public byte b;

        /// unsigned8->unsigned char
        public byte opacity;

        /// int32->int
        public int overlayAlgorithm;
    }

    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal struct PSPixelMap
    {
        public int version;
        public VRect bounds;
        public int imageMode;
        public int rowBytes;
        public int colBytes;
        public int planeBytes;
        public System.IntPtr baseAddr;
        public System.IntPtr mat;
        public System.IntPtr masks;
        public int maskPhaseRow;
        public int maskPhaseCol;
        public System.IntPtr pixelOverlays;
        public uint colorManagementOptions;
    }
}
