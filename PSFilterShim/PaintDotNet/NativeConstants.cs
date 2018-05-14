namespace PaintDotNet.SystemLayer
{
    internal static class NativeConstants
    {
        /// PAGE_READWRITE -> 0x04
        public const int PAGE_READWRITE = 4;

        public const uint MEM_COMMIT = 0x1000;
        public const uint MEM_RESERVE = 0x2000;
        public const uint MEM_RELEASE = 0x8000;

        public const int HeapCompatibilityInformation = 0;
    }
}
