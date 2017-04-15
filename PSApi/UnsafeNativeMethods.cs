/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2017 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
	[System.Security.SuppressUnmanagedCodeSecurity]
	internal static class UnsafeNativeMethods
	{
		[return: MarshalAs(UnmanagedType.Bool)]
		internal delegate bool EnumResNameDelegate(IntPtr hModule, IntPtr lpszType, IntPtr lpszName, IntPtr lParam);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
		internal static extern SafeLibraryHandle LoadLibraryExW(string lpFileName, IntPtr hFile, uint dwFlags);

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool EnumResourceNamesW(IntPtr hModule, string lpszType, EnumResNameDelegate lpEnumFunc, IntPtr lParam);

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
