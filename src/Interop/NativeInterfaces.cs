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

using System;
using System.Runtime.InteropServices;

namespace PSFilterPdn.Interop
{
    static class NativeInterfaces
    {
        /// <summary>The IShellLink interface allows Shell links to be created, modified, and resolved</summary>
        [ComImport(), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid(NativeConstants.IID_IShellLinkW)]
        internal interface IShellLinkW
        {
            /// <summary>Retrieves the path and file name of a Shell link object</summary>
            [PreserveSig]
            unsafe int GetPath(ushort* pszFile, int cchMaxPath, IntPtr pfd, uint fFlags);
            /// <summary>Retrieves the list of item identifiers for a Shell link object</summary>
            [PreserveSig]
            int GetIDList(out IntPtr ppidl);
            /// <summary>Sets the pointer to an item identifier list (PIDL) for a Shell link object.</summary>
            [PreserveSig]
            int SetIDList(IntPtr pidl);
            /// <summary>Retrieves the description string for a Shell link object</summary>
            [PreserveSig]
            unsafe int GetDescription(ushort* pszName, int cchMaxName);
            /// <summary>Sets the description for a Shell link object. The description can be any application-defined string</summary>
            [PreserveSig]
            int SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            /// <summary>Retrieves the name of the working directory for a Shell link object</summary>
            [PreserveSig]
            unsafe int GetWorkingDirectory(ushort* pszDir, int cchMaxPath);
            /// <summary>Sets the name of the working directory for a Shell link object</summary>
            [PreserveSig]
            int SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
            /// <summary>Retrieves the command-line arguments associated with a Shell link object</summary>
            [PreserveSig]
            unsafe int GetArguments(ushort* pszArgs, int cchMaxPath);
            /// <summary>Sets the command-line arguments for a Shell link object</summary>
            [PreserveSig]
            int SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
            /// <summary>Retrieves the hot key for a Shell link object</summary>
            [PreserveSig]
            int GetHotkey(out short pwHotkey);
            /// <summary>Sets a hot key for a Shell link object</summary>
            [PreserveSig]
            int SetHotkey(short wHotkey);
            /// <summary>Retrieves the show command for a Shell link object</summary>
            [PreserveSig]
            int GetShowCmd(out int piShowCmd);
            /// <summary>Sets the show command for a Shell link object. The show command sets the initial show state of the window.</summary>
            [PreserveSig]
            int SetShowCmd(int iShowCmd);
            /// <summary>Retrieves the location (path and index) of the icon for a Shell link object</summary>
            [PreserveSig]
            unsafe int GetIconLocation(ushort* pszIconPath, int cchIconPath, out int piIcon);
            /// <summary>Sets the location (path and index) of the icon for a Shell link object</summary>
            [PreserveSig]
            unsafe int SetIconLocation(ushort* pszIconPath, int iIcon);
            /// <summary>Sets the relative path to the Shell link object</summary>
            [PreserveSig]
            unsafe int SetRelativePath(ushort* pszPathRel, int dwReserved);
            /// <summary>Attempts to find the target of a Shell link, even if it has been moved or renamed</summary>
            [PreserveSig]
            int Resolve(IntPtr hwnd, uint fFlags);
            /// <summary>Sets the path and file name of a Shell link object</summary>
            [PreserveSig]
            unsafe int SetPath(ushort* pszFile);
        }

        [ComImport(), InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid(NativeConstants.IID_IPersist)]
        internal interface IPersist
        {
            [PreserveSig]
            int GetClassID(out Guid pClassID);
        }

        [ComImport(), Guid(NativeConstants.IID_IPersistFile), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IPersistFile : IPersist
        {
            [PreserveSig]
            new int GetClassID(out Guid pClassID);

            [PreserveSig]
            int IsDirty();

            [PreserveSig]
            unsafe int Load(ushort* pszFileName, uint dwMode);

            [PreserveSig]
            unsafe int Save(ushort* pszFileName, [In, MarshalAs(UnmanagedType.Bool)] bool fRemember);

            [PreserveSig]
            unsafe int SaveCompleted(ushort* pszFileName);

            [PreserveSig]
            unsafe int GetCurFile(ushort* ppszFileName);
        }
    }
}
