using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PaintDotNet.SystemLayer
{
    internal static class NativeConstants
    {
        public const int WM_SETREDRAW = 0x000B;

        /// PAGE_NOACCESS -> 0x01
        public const int PAGE_NOACCESS = 1;

        /// PAGE_READONLY -> 0x02
        public const int PAGE_READONLY = 2;

        /// PAGE_READWRITE -> 0x04
        public const int PAGE_READWRITE = 4;

        public const uint MEM_COMMIT = 0x1000;
        public const uint MEM_RESERVE = 0x2000;
        public const uint MEM_RELEASE = 0x8000;
        public const uint DIB_RGB_COLORS = 0; /* color table in RGBs */

        public const uint BI_RGB = 0;
        public const int HeapCompatibilityInformation = 0;

    }
}
