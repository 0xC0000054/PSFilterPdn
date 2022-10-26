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

using PaintDotNet.Imaging;

namespace PSFilterPdn.Metadata
{
    internal static class ExifValueTypeUtil
    {
        /// <summary>
        /// Determines whether the values fit in the offset field.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="count">The count.</param>
        /// <returns>
        /// <see langword="true"/> if the values fit in the offset field; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool ValueFitsInOffsetField(ExifValueType type, uint count)
        {
            switch (type)
            {
                case ExifValueType.Byte:
                case ExifValueType.Ascii:
                case ExifValueType.Undefined:
                case (ExifValueType)6: // SByte
                    return count <= 4;
                case ExifValueType.Short:
                case ExifValueType.SShort:
                    return count <= 2;
                case ExifValueType.Long:
                case ExifValueType.SLong:
                case ExifValueType.Float:
                case (ExifValueType)13: // IFD
                    return count <= 1;
                case ExifValueType.Rational:
                case ExifValueType.SRational:
                case ExifValueType.Double:
                default:
                    return false;
            }
        }
    }
}
