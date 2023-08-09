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

/* Adapted from ASZStringSuite.h, PIBufferSuite.h, PIColorSpaceSuite.h, PIHandleSuite.h, PIUIHooskSuite.h, SPPlugs.h
*  Copyright 1986 - 2000 Adobe Systems Incorporated
*  All Rights Reserved
*/

using System;

namespace PSFilterLoad.PSApi.PICA
{
    internal struct PSBufferSuite1
    {
        public UnmanagedFunctionPointer<PSBufferSuiteNew> New;
        public UnmanagedFunctionPointer<PSBufferSuiteDispose> Dispose;
        public UnmanagedFunctionPointer<PSBufferSuiteGetSize> GetSize;
        public UnmanagedFunctionPointer<PSBufferSuiteGetSpace> GetSpace;
    }

#pragma warning disable 0649
    internal struct CS_XYZ
    {
        public ushort x; // all clamped to between 0 and 255, why is a ushort used instead of a byte?
        public ushort y;
        public ushort z;

        public override readonly string ToString()
        {
            return string.Format("X: {0}, Y: {1}, Z: {2}", x, y, z);
        }
    }

    internal struct CS_Color8
    {
        public byte c0;
        public byte c1;
        public byte c2;
        public byte c3;
    }

    internal struct CS_Color16
    {
        public ushort c0;
        public ushort c1;
        public ushort c2;
        public ushort c3;
    }
#pragma warning restore 0649

    internal struct ColorID : IEquatable<ColorID>
    {
        private readonly IntPtr value;

        public ColorID(int index)
        {
            value = new IntPtr(index);
        }

        public readonly int Index => value.ToInt32();

        public override bool Equals(object obj)
        {
            return obj is ColorID other && Equals(other);
        }

        public readonly bool Equals(ColorID other)
        {
            return value == other.value;
        }

        public override readonly int GetHashCode()
        {
            return -1584136870 + value.GetHashCode();
        }

        public override string ToString()
        {
            return Index.ToString();
        }

        public static bool operator ==(ColorID left, ColorID right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ColorID left, ColorID right)
        {
            return !left.Equals(right);
        }
    }

    internal struct PSColorSpaceSuite1
    {
        public UnmanagedFunctionPointer<CSMake> Make;
        public UnmanagedFunctionPointer<CSDelete> Delete;
        public UnmanagedFunctionPointer<CSStuffComponents> StuffComponents;
        public UnmanagedFunctionPointer<CSExtractComponents> ExtractComponents;
        public UnmanagedFunctionPointer<CSStuffXYZ> StuffXYZ;
        public UnmanagedFunctionPointer<CSExtractXYZ> ExtractXYZ;
        public UnmanagedFunctionPointer<CSConvert8> Convert8;
        public UnmanagedFunctionPointer<CSConvert16> Convert16;
        public UnmanagedFunctionPointer<CSGetNativeSpace> GetNativeSpace;
        public UnmanagedFunctionPointer<CSIsBookColor> IsBookColor;
        public UnmanagedFunctionPointer<CSExtractColorName> ExtractColorName;
        public UnmanagedFunctionPointer<CSPickColor> PickColor;
        public UnmanagedFunctionPointer<CSConvert> Convert8to16;
        public UnmanagedFunctionPointer<CSConvert> Convert16to8;
        public UnmanagedFunctionPointer<CSConvertToMonitorRGB> ConvertToMonitorRGB;
    }

    internal struct PSHandleSuite1
    {
        public UnmanagedFunctionPointer<NewPIHandleProc> New;
        public UnmanagedFunctionPointer<DisposePIHandleProc> Dispose;
        public UnmanagedFunctionPointer<SetPIHandleLockDelegate> SetLock;
        public UnmanagedFunctionPointer<GetPIHandleSizeProc> GetSize;
        public UnmanagedFunctionPointer<SetPIHandleSizeProc> SetSize;
        public UnmanagedFunctionPointer<RecoverSpaceProc> RecoverSpace;
    }

    internal struct PSHandleSuite2
    {
        public UnmanagedFunctionPointer<NewPIHandleProc> New;
        public UnmanagedFunctionPointer<DisposePIHandleProc> Dispose;
        public UnmanagedFunctionPointer<DisposeRegularPIHandleProc> DisposeRegularHandle;
        public UnmanagedFunctionPointer<SetPIHandleLockDelegate> SetLock;
        public UnmanagedFunctionPointer<GetPIHandleSizeProc> GetSize;
        public UnmanagedFunctionPointer<SetPIHandleSizeProc> SetSize;
        public UnmanagedFunctionPointer<RecoverSpaceProc> RecoverSpace;
    }

    internal struct PSUIHooksSuite1
    {
        public UnmanagedFunctionPointer<ProcessEventProc> processEvent;
        public UnmanagedFunctionPointer<DisplayPixelsProc> displayPixels;
        public UnmanagedFunctionPointer<ProgressProc> progressBar;
        public UnmanagedFunctionPointer<TestAbortProc> testAbort;
        public UnmanagedFunctionPointer<UISuiteMainWindowHandle> MainAppWindow;
        public UnmanagedFunctionPointer<UISuiteHostSetCursor> SetCursor;
        public UnmanagedFunctionPointer<UISuiteHostTickCount> TickCount;
        public UnmanagedFunctionPointer<UISuiteGetPluginName> GetPluginName;
    }

    internal struct ASZString : IEquatable<ASZString>
    {
        private readonly IntPtr value;

        public ASZString(int index)
        {
            value = new IntPtr(index);
        }

        public readonly int Index => value.ToInt32();

        public override bool Equals(object obj)
        {
            return obj is ASZString other && Equals(other);
        }

        public readonly bool Equals(ASZString other)
        {
            return value == other.value;
        }

        public override readonly int GetHashCode()
        {
            return -1584136870 + value.GetHashCode();
        }

        public override string ToString()
        {
            return Index.ToString();
        }

        public static bool operator ==(ASZString left, ASZString right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ASZString left, ASZString right)
        {
            return !left.Equals(right);
        }
    }

    internal struct ASZStringSuite1
    {
        public UnmanagedFunctionPointer<ASZStringMakeFromUnicode> MakeFromUnicode;
        public UnmanagedFunctionPointer<ASZStringMakeFromCString> MakeFromCString;
        public UnmanagedFunctionPointer<ASZStringMakeFromPascalString> MakeFromPascalString;
        public UnmanagedFunctionPointer<ASZStringMakeRomanizationOfInteger> MakeRomanizationOfInteger;
        public UnmanagedFunctionPointer<ASZStringMakeRomanizationOfFixed> MakeRomanizationOfFixed;
        public UnmanagedFunctionPointer<ASZStringMakeRomanizationOfDouble> MakeRomanizationOfDouble;
        public UnmanagedFunctionPointer<ASZStringGetEmpty> GetEmpty;
        public UnmanagedFunctionPointer<ASZStringCopy> Copy;
        public UnmanagedFunctionPointer<ASZStringReplace> Replace;
        public UnmanagedFunctionPointer<ASZStringTrimEllipsis> TrimEllipsis;
        public UnmanagedFunctionPointer<ASZStringTrimSpaces> TrimSpaces;
        public UnmanagedFunctionPointer<ASZStringRemoveAccelerators> RemoveAccelerators;
        public UnmanagedFunctionPointer<ASZStringAddRef> AddRef;
        public UnmanagedFunctionPointer<ASZStringRelease> Release;
        public UnmanagedFunctionPointer<ASZStringIsAllWhiteSpace> IsAllWhiteSpace;
        public UnmanagedFunctionPointer<ASZStringIsEmpty> IsEmpty;
        public UnmanagedFunctionPointer<ASZStringWillReplace> WillReplace;
        public UnmanagedFunctionPointer<ASZStringLengthAsUnicodeCString> LengthAsUnicodeCString;
        public UnmanagedFunctionPointer<ASZStringAsUnicodeCString> AsUnicodeCString;
        public UnmanagedFunctionPointer<ASZStringLengthAsCString> LengthAsCString;
        public UnmanagedFunctionPointer<ASZStringAsCString> AsCString;
        public UnmanagedFunctionPointer<ASZStringLengthAsPascalString> LengthAsPascalString;
        public UnmanagedFunctionPointer<ASZStringAsPascalString> AsPascalString;
    }
}
