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

using System.IO;

namespace PSFilterShim
{
    internal static class StreamExtensions
    {
        public static void ProperRead(this Stream stream, byte[] bytes, int offset, int count)
        {
            int totalBytesRead = 0;

            do
            {
                int bytesRead = stream.Read(bytes, offset + totalBytesRead, count - totalBytesRead);

                if (bytesRead == 0)
                {
                    throw new EndOfStreamException();
                }

                totalBytesRead += bytesRead;

            } while (totalBytesRead < count);
        }
    }
}
