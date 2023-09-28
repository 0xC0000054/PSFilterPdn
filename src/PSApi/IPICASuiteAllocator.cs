/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
//   Copyright (C) 2010-2023 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;

namespace PSFilterLoad.PSApi
{
    internal interface IPICASuiteAllocator
    {
        /// <summary>
        /// Allocates the suite in native memory.
        /// </summary>
        /// <param name="version">The version of the suite to allocate.</param>
        /// <returns>A pointer to the allocated suite.</returns>
        /// <remarks>
        /// The native memory must be freed with <see cref="Memory.Free(nint)"/>.
        /// </remarks>
        /// <exception cref="OutOfMemoryException">
        /// Failed to allocate native memory for the suite.
        /// </exception>
        /// <exception cref="UnsupportedPICASuiteVersionException">
        /// The PICA suite version is not supported.
        /// </exception>
        IntPtr Allocate(int version);

        /// <summary>
        /// Determines whether the specified suite version is supported.
        /// </summary>
        /// <param name="version">The suite version.</param>
        /// <returns>
        /// <see langword="true"/> if the suite version is supported; otherwise, <see langword="false"/>.
        /// </returns>
        bool IsSupportedVersion(int version);
    }
}
