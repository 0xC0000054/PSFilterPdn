/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2022 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

namespace PSFilterLoad.PSApi
{
    internal static class NativeConstants
    {
        public const int GPTR = 64;

        public const int LOAD_LIBRARY_AS_DATAFILE = 2;

        public const uint HEAP_ZERO_MEMORY = 8;

        public const int PAGE_NOACCESS = 1;
        public const int PAGE_READONLY = 2;
        public const int PAGE_READWRITE = 4;
        public const int PAGE_WRITECOPY = 8;
        public const int PAGE_EXECUTE = 16;
        public const int PAGE_EXECUTE_READ = 32;
        public const int PAGE_EXECUTE_READWRITE = 64;
        public const int PAGE_EXECUTE_WRITECOPY = 128;
        public const int PAGE_GUARD = 256;

        public const uint MEM_COMMIT = 0x1000;
        public const uint MEM_RELEASE = 0x8000;

        internal const uint CC_RGBINIT = 0x00000001;
        internal const uint CC_FULLOPEN = 0x00000002;
        internal const uint CC_ENABLEHOOK = 0x00000010;
        internal const uint CC_SOLIDCOLOR = 0x00000080;

        internal const int WM_INITDIALOG = 0x0110;
    }
}
