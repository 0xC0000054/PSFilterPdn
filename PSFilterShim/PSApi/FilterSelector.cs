/* Adapted from PIFilter.h
 * Copyright (c) 1990-1, Thomas Knoll.
 * Copyright (c) 1992-6, Adobe Systems Incorporated.
 * All rights reserved.
*/


namespace PSFilterLoad.PSApi
{
    internal enum FilterSelector : short
    {
        About = 0,
        Parameters = 1,
        Prepare = 2,
        Start = 3,
        Continue = 4,
        Finish = 5
    }
}
