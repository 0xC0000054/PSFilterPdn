/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2023 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

namespace PSFilterLoad.PSApi
{
    internal static partial class PluginLoader
    {
        private sealed class FilterCaseInfoResult
        {
            public readonly FilterCaseInfoCollection filterCaseInfo;
            public readonly int propertyLength;

            public FilterCaseInfoResult(FilterCaseInfoCollection filterCaseInfo, int actualArrayLength)
            {
                this.filterCaseInfo = filterCaseInfo;
                propertyLength = actualArrayLength;
            }
        }

        private static class FilterCaseInfoParser
        {
            public static unsafe FilterCaseInfoResult Parse(byte* ptr, int length)
            {
                const int MinLength = 7 * FilterCaseInfo.SizeOf;

                if (length < MinLength)
                {
                    return null;
                }

                FilterCaseInfo[] info = new FilterCaseInfo[7];
                int offset = 0;
                bool filterInfoValid = true;

                for (int i = 0; i < info.Length; i++)
                {
                    byte? inputHandling = ParseField(ptr, offset, out int bytesRead);
                    offset += bytesRead;

                    byte? outputHandling = ParseField(ptr, offset, out bytesRead);
                    offset += bytesRead;

                    byte? flags1 = ParseField(ptr, offset, out bytesRead);
                    offset += bytesRead;

                    byte? flags2 = ParseField(ptr, offset, out bytesRead);
                    offset += bytesRead;

                    if (inputHandling.HasValue &&
                        outputHandling.HasValue &&
                        flags1.HasValue &&
                        flags2.HasValue)
                    {
                        info[i] = new FilterCaseInfo((FilterDataHandling)inputHandling.Value,
                                                     (FilterDataHandling)outputHandling.Value,
                                                     (FilterCaseInfoFlags)flags1.Value,
                                                     flags2.Value);
                    }
                    else
                    {
                        filterInfoValid = false;
                    }
                }

                return new FilterCaseInfoResult(filterInfoValid ? new FilterCaseInfoCollection(info) : null, offset);
            }

            private static bool IsHexadecimalChar(char value)
            {
                return value >= '0' && value <= '9' ||
                       value >= 'A' && value <= 'F' ||
                       value >= 'a' && value <= 'f';
            }

            private static unsafe byte? ParseField(byte* data, int startOffset, out int fieldLength)
            {
                byte value = data[startOffset];

                char c = (char)value;
                // The FilterCaseInfo resource in Alf's Power Toys contains incorrectly escaped hexadecimal numbers.
                // The numbers are formatted /x00 instead of \x00.
                if (c == '/')
                {
                    char next = (char)data[startOffset + 1];
                    if (next == 'x')
                    {
                        int offset = startOffset + 2;
                        // Convert the hexadecimal characters to a decimal number.
                        char hexChar = (char)data[offset];

                        if (IsHexadecimalChar(hexChar))
                        {
                            int fieldValue = 0;

                            do
                            {
                                int digit;

                                if (hexChar < 'A')
                                {
                                    digit = hexChar - '0';
                                }
                                else
                                {
                                    if (hexChar >= 'a')
                                    {
                                        // Convert the letter to upper case.
                                        hexChar = (char)(hexChar - 0x20);
                                    }

                                    digit = 10 + (hexChar - 'A');
                                }

                                fieldValue = (fieldValue * 16) + digit;

                                offset++;
                                hexChar = (char)data[offset];

                            } while (IsHexadecimalChar(hexChar));

                            if (fieldValue >= byte.MinValue && fieldValue <= byte.MaxValue)
                            {
                                fieldLength = offset - startOffset;

                                return (byte)fieldValue;
                            }
                        }

                        fieldLength = 2;
                        return null;
                    }
                }

                fieldLength = 1;
                return value;
            }
        }
    }
}
