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

namespace PSFilterPdn.Interop
{
    internal static class NativeConstants
    {
        internal const int MAX_PATH = 260;

        internal const int S_OK = 0;
        internal const int S_FALSE = 1;

        internal const int LOGPIXELSX = 88;

        internal const int ERROR_SUCCESS = 0;
        internal const int ERROR_RESOURCE_ENUM_USER_STOP = 0x00003B02;
        internal const int ERROR_RESOURCE_DATA_NOT_FOUND = 0x00000714;
        internal const int ERROR_RESOURCE_TYPE_NOT_FOUND = 0x00000715;
        internal const int ERROR_RESOURCE_NAME_NOT_FOUND = 0x00000716;
        internal const int ERROR_RESOURCE_LANG_NOT_FOUND = 0x00000717;

        internal const uint FILE_ATTRIBUTE_DIRECTORY = 16U;
        internal const uint FILE_ATTRIBUTE_REPARSE_POINT = 1024U;

        internal const uint INVALID_FILE_ATTRIBUTES = 0xFFFFFFFF;

        internal const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;

        internal const uint FILE_SHARE_READ = 0x00000001;
        internal const uint FILE_SHARE_WRITE = 0x00000002;
        internal const uint FILE_SHARE_DELETE = 0x00000004;

        internal const uint GENERIC_READ = 0x80000000;

        internal const uint OPEN_EXISTING = 3;

        internal const uint LOAD_LIBRARY_AS_DATAFILE = 2;
        internal const uint LOAD_LIBRARY_AS_IMAGE_RESOURCE = 0x00000020;

        internal const ushort PROCESSOR_ARCHITECTURE_INTEL = 0;
        internal const ushort PROCESSOR_ARCHITECTURE_AMD64 = 9;
        internal const ushort PROCESSOR_ARCHITECTURE_ARM = 5;
        internal const ushort PROCESSOR_ARCHITECTURE_ARM64 = 12;

        internal const uint SEM_FAILCRITICALERRORS = 1U;

        internal const int STGM_READ = 0;

        internal const uint CC_RGBINIT = 0x00000001;
        internal const uint CC_FULLOPEN = 0x00000002;
        internal const uint CC_ENABLEHOOK = 0x00000010;
        internal const uint CC_SOLIDCOLOR = 0x00000080;

        internal const int WM_INITDIALOG = 0x0110;
        internal const int WM_PRINTCLIENT = 0x0318;
        internal const int WM_THEMECHANGED = 0x031A;

        internal const int PRF_CLIENT = 0x00000004;

        internal const int TV_FIRST = 0x1100;
        internal const int TVM_SETEXTENDEDSTYLE = TV_FIRST + 44;
        internal const int TVS_EX_DOUBLEBUFFER = 0x0004;

        internal const int TCM_FIRST = 0x1300;
        internal const int TCM_HITTEST = TCM_FIRST + 13;

        internal const string CLSID_ShellLink = "00021401-0000-0000-C000-000000000046";

        internal const string IID_IPersist = "0000010c-0000-0000-c000-000000000046";
        internal const string IID_IPersistFile = "0000010b-0000-0000-C000-000000000046";
        internal const string IID_IShellLinkW = "000214F9-0000-0000-C000-000000000046";
    }
}
