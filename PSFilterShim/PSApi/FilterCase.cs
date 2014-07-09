/* Adapted from PIFilter.h
 * Copyright (c) 1990-1, Thomas Knoll.
 * Copyright (c) 1992-6, Adobe Systems Incorporated.
 * All rights reserved.
*/

namespace PSFilterLoad.PSApi
{
    static class FilterCase
    {
        public const short Unsupported = -1;
        public const short FlatImageNoSelection = 1;
        public const short FlatImageWithSelection = 2;
        public const short FloatingSelection = 3;
        public const short EditableTransparencyNoSelection = 4;
        public const short EditableTransparencyWithSelection = 5;
        public const short ProtectedTransparencyNoSelection = 6;
        public const short ProtectedTransparencyWithSelection = 7;
    }
}