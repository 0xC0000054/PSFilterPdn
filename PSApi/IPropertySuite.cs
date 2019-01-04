/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2019 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

namespace PSFilterLoad.PSApi
{
    interface IPropertySuite
    {
        /// <summary>
        /// Creates the property suite structure.
        /// </summary>
        /// <returns>A <see cref="PropertyProcs"/> structure.</returns>
        PropertyProcs CreatePropertySuite();
    }
}
