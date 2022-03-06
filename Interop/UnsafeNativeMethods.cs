﻿/////////////////////////////////////////////////////////////////////////////////
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

using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;

namespace PSFilterPdn.Interop
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

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern SafeFileHandle CreateFileW(
               [In(), MarshalAs(UnmanagedType.LPWStr)] string lpFileName,
               [In()] uint dwDesiredAccess,
               [In()] uint dwShareMode,
               [In()] IntPtr lpSecurityAttributes,
               [In()] uint dwCreationDisposition,
               [In()] uint dwFlagsAndAttributes,
               [In()] IntPtr hTemplateFile);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetFileInformationByHandle([In()] SafeFileHandle hFile, [Out()] out NativeStructs.BY_HANDLE_FILE_INFORMATION lpFileInformation);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern unsafe bool GetFileInformationByHandleEx(
            [In()] SafeFileHandle hFile,
            [In()] NativeEnums.FILE_INFO_BY_HANDLE_CLASS infoClass,
            [Out()] void* lpFileInformation,
            [In()] uint dwBufferSize);

        [return: MarshalAs(UnmanagedType.Bool)]
        internal delegate bool EnumResNameDelegate(IntPtr hModule, IntPtr lpszType, IntPtr lpszName, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
        internal static extern SafeLibraryHandle LoadLibraryExW(string lpFileName, IntPtr hFile, uint dwFlags);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EnumResourceNamesW(SafeLibraryHandle hModule, string lpszType, EnumResNameDelegate lpEnumFunc, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        internal static extern IntPtr FindResourceW(IntPtr hModule, IntPtr lpName, IntPtr lpType);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        internal static extern IntPtr LoadResource(IntPtr hModule, IntPtr hResource);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        internal static extern IntPtr LockResource(IntPtr hGlobal);

        [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
        [DllImport("kernel32.dll", ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, BestFitMapping = false)]
        internal static extern IntPtr GetProcAddress(SafeLibraryHandle hModule, string lpProcName);
    }
}
