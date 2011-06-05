namespace PSFilterLoad.PSApi
{
    static class NativeConstants
    {
        public const int GPTR = 64;

        /// GMEM_MOVEABLE -> 0x0002
        public const int GMEM_MOVEABLE = 2;
        /// LOAD_LIBRARY_AS_DATAFILE -> 0x00000002
        public const int LOAD_LIBRARY_AS_DATAFILE = 2;
        public const int RT_DIALOG = 5;



        /// PAGE_NOACCESS -> 0x01
        public const int PAGE_NOACCESS = 1;

        /// PAGE_READONLY -> 0x02
        public const int PAGE_READONLY = 2;

        /// PAGE_READWRITE -> 0x04
        public const int PAGE_READWRITE = 4;

        /// PAGE_WRITECOPY -> 0x08
        public const int PAGE_WRITECOPY = 8;

        /// PAGE_EXECUTE -> 0x10
        public const int PAGE_EXECUTE = 16;

        /// PAGE_EXECUTE_READ -> 0x20
        public const int PAGE_EXECUTE_READ = 32;

        /// PAGE_EXECUTE_READWRITE -> 0x40
        public const int PAGE_EXECUTE_READWRITE = 64;

        /// PAGE_EXECUTE_WRITECOPY -> 0x80
        public const int PAGE_EXECUTE_WRITECOPY = 128;

        /// PAGE_GUARD -> 0x100
        public const int PAGE_GUARD = 256;
    }
}
