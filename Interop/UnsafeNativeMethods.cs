﻿/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2018 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;

namespace PSFilterPdn
{
    [SuppressUnmanagedCodeSecurity]
    internal static class UnsafeNativeMethods
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern SafeFindHandle FindFirstFileExW(
            string fileName,
            NativeEnums.FindExInfoLevel fInfoLevelId,
            [Out()] WIN32_FIND_DATAW data,
            NativeEnums.FindExSearchOp fSearchOp,
            IntPtr lpSearchFilter,
            NativeEnums.FindExAdditionalFlags dwAdditionalFlags);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool FindNextFileW(SafeFindHandle hndFindFile, [Out()] WIN32_FIND_DATAW lpFindFileData);

        [DllImport("kernel32.dll", ExactSpelling = true), ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool FindClose(IntPtr handle);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern uint GetFileAttributesW(string lpFileName);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        internal static extern uint SetErrorMode(uint uMode);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool SetThreadErrorMode(uint dwNewMode, out uint lpOldMode);
    }
}
