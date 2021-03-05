/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2021 Nicholas Hayes
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
        public IntPtr New;
        public IntPtr Dispose;
        public IntPtr GetSize;
        public IntPtr GetSpace;
    }

#pragma warning disable 0649
    internal struct CS_XYZ
    {
        public ushort x; // all clamped to between 0 and 255, why is a ushort used instead of a byte?
        public ushort y;
        public ushort z;
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

        public int Index => value.ToInt32();

        public override bool Equals(object obj)
        {
            return obj is ColorID other && Equals(other);
        }

        public bool Equals(ColorID other)
        {
            return value == other.value;
        }

        public override int GetHashCode()
        {
            return -1584136870 + value.GetHashCode();
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
        public IntPtr Make;
        public IntPtr Delete;
        public IntPtr StuffComponents;
        public IntPtr ExtractComponents;
        public IntPtr StuffXYZ;
        public IntPtr ExtractXYZ;
        public IntPtr Convert8;
        public IntPtr Convert16;
        public IntPtr GetNativeSpace;
        public IntPtr IsBookColor;
        public IntPtr ExtractColorName;
        public IntPtr PickColor;
        public IntPtr Convert8to16;
        public IntPtr Convert16to8;
        public IntPtr ConvertToMonitorRGB;
    }

    internal struct PSHandleSuite1
    {
        public IntPtr New;
        public IntPtr Dispose;
        public IntPtr SetLock;
        public IntPtr GetSize;
        public IntPtr SetSize;
        public IntPtr RecoverSpace;
    }
    internal struct PSHandleSuite2
    {
        public IntPtr New;
        public IntPtr Dispose;
        public IntPtr DisposeRegularHandle;
        public IntPtr SetLock;
        public IntPtr GetSize;
        public IntPtr SetSize;
        public IntPtr RecoverSpace;
    }

    internal struct PSUIHooksSuite1
    {
        public IntPtr processEvent;
        public IntPtr displayPixels;
        public IntPtr progressBar;
        public IntPtr testAbort;
        public IntPtr MainAppWindow;
        public IntPtr SetCursor;
        public IntPtr TickCount;
        public IntPtr GetPluginName;
    }

#if PICASUITEDEBUG
    internal struct SPPluginsSuite4
    {
        public IntPtr AllocatePluginList;
        public IntPtr FreePluginList;

        public IntPtr AddPlugin;

        public IntPtr NewPluginListIterator;
        public IntPtr NextPlugin;
        public IntPtr DeletePluginListIterator;
        public IntPtr GetPluginListNeededSuiteAvailable;

        public IntPtr GetPluginHostEntry;
        public IntPtr GetPluginFileSpecification;
        public IntPtr GetPluginPropertyList;
        public IntPtr GetPluginGlobals;
        public IntPtr SetPluginGlobals;
        public IntPtr GetPluginStarted;
        public IntPtr SetPluginStarted;
        public IntPtr GetPluginSkipShutdown;
        public IntPtr SetPluginSkipShutdown;
        public IntPtr GetPluginBroken;
        public IntPtr SetPluginBroken;
        public IntPtr GetPluginAdapter;
        public IntPtr GetPluginAdapterInfo;
        public IntPtr SetPluginAdapterInfo;

        public IntPtr FindPluginProperty;

        public IntPtr GetPluginName;
        public IntPtr SetPluginName;
        public IntPtr GetNamedPlugin;

        public IntPtr SetPluginPropertyList;
    }
#endif

    internal struct ASZString : IEquatable<ASZString>
    {
        private readonly IntPtr value;

        public ASZString(int index)
        {
            value = new IntPtr(index);
        }

        public int Index => value.ToInt32();

        public override bool Equals(object obj)
        {
            return obj is ASZString other && Equals(other);
        }

        public bool Equals(ASZString other)
        {
            return value == other.value;
        }

        public override int GetHashCode()
        {
            return -1584136870 + value.GetHashCode();
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
        public IntPtr MakeFromUnicode;
        public IntPtr MakeFromCString;
        public IntPtr MakeFromPascalString;
        public IntPtr MakeRomanizationOfInteger;
        public IntPtr MakeRomanizationOfFixed;
        public IntPtr MakeRomanizationOfDouble;
        public IntPtr GetEmpty;
        public IntPtr Copy;
        public IntPtr Replace;
        public IntPtr TrimEllipsis;
        public IntPtr TrimSpaces;
        public IntPtr RemoveAccelerators;
        public IntPtr AddRef;
        public IntPtr Release;
        public IntPtr IsAllWhiteSpace;
        public IntPtr IsEmpty;
        public IntPtr WillReplace;
        public IntPtr LengthAsUnicodeCString;
        public IntPtr AsUnicodeCString;
        public IntPtr LengthAsCString;
        public IntPtr AsCString;
        public IntPtr LengthAsPascalString;
        public IntPtr AsPascalString;
    }
}
