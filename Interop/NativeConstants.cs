﻿/////////////////////////////////////////////////////////////////////////////////
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

namespace PSFilterPdn
{
    internal static class NativeConstants
    {
        internal const int MAX_PATH = 260;

        internal const int S_OK = 0;
        internal const int S_FALSE = 1;

        internal const int LOGPIXELSX = 88;

        internal const uint FILE_ATTRIBUTE_DIRECTORY = 16U;
        internal const uint FILE_ATTRIBUTE_REPARSE_POINT = 1024U;

        internal const uint INVALID_FILE_ATTRIBUTES = 0xFFFFFFFF;

        internal const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;

        internal const uint FILE_SHARE_READ = 0x00000001;
        internal const uint FILE_SHARE_WRITE = 0x00000002;
        internal const uint FILE_SHARE_DELETE = 0x00000004;

        internal const uint GENERIC_READ = 0x80000000;

        internal const uint OPEN_EXISTING = 3;

        internal const uint SEM_FAILCRITICALERRORS = 1U;

        internal const int STGM_READ = 0;

        internal const int WM_PRINTCLIENT = 0x0318;
        internal const int WM_THEMECHANGED = 0x031A;

        internal const int PRF_CLIENT = 0x00000004;

        internal const int TV_FIRST = 0x1100;
        internal const int TVM_SETEXTENDEDSTYLE = TV_FIRST + 44;
        internal const int TVS_EX_DOUBLEBUFFER = 0x0004;

        internal const int TCM_FIRST = 0x1300;
        internal const int TCM_HITTEST = TCM_FIRST + 13;

        internal const string CLSID_FileOpenDialog = "DC1C5A9C-E88A-4dde-A5A1-60F82A20AEF7";
        internal const string CLSID_ShellLink = "00021401-0000-0000-C000-000000000046";

        internal const string IID_IModalWindow = "b4db1657-70d7-485e-8e3e-6fcb5a5c1802";
        internal const string IID_IFileDialog = "42f85136-db7e-439c-85f1-e4075d135fc8";
        internal const string IID_IFileOpenDialog = "d57c7288-d4ad-4768-be02-9d969532d960";
        internal const string IID_IFileDialogEvents = "973510DB-7D7F-452B-8975-74A85828D354";
        internal const string IID_IFileDialogControlEvents = "36116642-D713-4b97-9B83-7484A9D00433";
        internal const string IID_IFileDialogCustomize = "8016b7b3-3d49-4504-a0aa-2a37494e606f";
        internal const string IID_IShellItem = "43826D1E-E718-42EE-BC55-A1E261C37BFE";
        internal const string IID_IShellItemArray = "B63EA76D-1F85-456F-A19C-48159EFA858B";
        internal const string IID_IPersist = "0000010c-0000-0000-c000-000000000046";
        internal const string IID_IPersistFile = "0000010b-0000-0000-C000-000000000046";
        internal const string IID_IShellLinkW = "000214F9-0000-0000-C000-000000000046";
    }
}
