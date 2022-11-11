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

using System;

namespace PSFilterPdn.Interop
{
    internal static class NativeEnums
    {
#pragma warning disable RCS1135 // Declare enum member with zero value (when enum has FlagsAttribute).
#pragma warning disable RCS1154 // Sort enum members.
#pragma warning disable RCS1191 // Declare enum value as combination of names.

        [Flags]
        internal enum ProcessDEPPolicy : uint
        {
            PROCESS_DEP_DISABLED = 0,
            PROCESS_DEP_ENABLE = 1,
            PROCESS_DEP_DISABLE_ATL_THUNK_EMULATION = 2
        }

        [Flags]
        internal enum TCHITTESTFLAGS
        {
            TCHT_NOWHERE = 1,
            TCHT_ONITEMICON = 2,
            TCHT_ONITEMLABEL = 4,
            TCHT_ONITEM = TCHT_ONITEMICON | TCHT_ONITEMLABEL
        }

        internal enum FindExInfoLevel : int
        {
            Standard = 0,
            Basic
        }

        internal enum FindExSearchOp : int
        {
            NameMatch = 0,
            LimitToDirectories,
            LimitToDevices
        }

        [Flags]
        internal enum FindExAdditionalFlags : uint
        {
            None = 0,
            CaseSensitive = 1,
            LargeFetch = 2
        }

        internal enum FILE_INFO_BY_HANDLE_CLASS
        {
            FileBasicInfo = 0,
            FileStandardInfo = 1,
            FileNameInfo = 2,
            FileRenameInfo = 3,
            FileDispositionInfo = 4,
            FileAllocationInfo = 5,
            FileEndOfFileInfo = 6,
            FileStreamInfo = 7,
            FileCompressionInfo = 8,
            FileAttributeTagInfo = 9,
            FileIdBothDirectoryInfo = 10,// 0x0A
            FileIdBothDirectoryRestartInfo = 11, // 0xB
            FileIoPriorityHintInfo = 12, // 0xC
            FileRemoteProtocolInfo = 13, // 0xD
            FileFullDirectoryInfo = 14, // 0xE
            FileFullDirectoryRestartInfo = 15, // 0xF
            FileStorageInfo = 16, // 0x10
            FileAlignmentInfo = 17, // 0x11
            FileIdInfo = 18, // 0x12
            FileIdExtdDirectoryInfo = 19, // 0x13
            FileIdExtdDirectoryRestartInfo = 20, // 0x14
        }

#pragma warning restore RCS1135 // Declare enum member with zero value (when enum has FlagsAttribute).
#pragma warning restore RCS1154 // Sort enum members.
#pragma warning restore RCS1191 // Declare enum value as combination of names.
    }
}

