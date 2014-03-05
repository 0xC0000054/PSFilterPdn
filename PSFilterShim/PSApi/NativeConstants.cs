﻿namespace PSFilterLoad.PSApi
{
    static class NativeConstants
    {
        public const int GPTR = 64;

        public const int LOAD_LIBRARY_AS_DATAFILE = 2;

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
    }
}
