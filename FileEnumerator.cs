using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using Microsoft.Win32.SafeHandles;

namespace PSFilterPdn
{
    /// <summary>
    /// Enumerates through a directory using the native API.
    /// </summary>
    internal static class FileEnumerator
    {
        private sealed class SafeFindHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            private SafeFindHandle() : base(true) { }

            protected override bool ReleaseHandle()
            {
                return UnsafeNativeMethods.FindClose(handle);
            }
        }

        private enum FindExInfoLevels : int
        {
            Standard = 0,
            Basic,
            MaxInfoLevel,
        }

        private enum FindExSearchOps : int
        {
            NameMatch = 0,
            LimitToDirectories,
            LimitToDevices,
            MaxSearchOp,
        }

        [Flags]
        private enum FindExAdditionalFlags : uint
        {
            None = 0,
            CaseSensitive = 1,
            LargeFetch = 2
        }

        [SuppressUnmanagedCodeSecurity]
        private static class UnsafeNativeMethods
        {
            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
            internal static extern SafeFindHandle FindFirstFileExW([In(), MarshalAs(UnmanagedType.LPWStr)] string fileName, [In()] FindExInfoLevels fInfoLevelId, out WIN32_FIND_DATAW data, [In()] FindExSearchOps fSearchOp, [In()] IntPtr lpSearchFilter, [In()] FindExAdditionalFlags dwAdditionalFlags);

            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool FindNextFileW([In()] SafeFindHandle hndFindFile, out WIN32_FIND_DATAW lpFindFileData);

            [DllImport("kernel32.dll", ExactSpelling = true), ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool FindClose([In()] IntPtr handle);
        }

        private static class NativeConstants
        {
            internal const uint FILE_ATTRIBUTE_DIRECTORY = 16U;
            internal const uint FILE_ATTRIBUTE_REPARSE_POINT = 1024U;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WIN32_FIND_DATAW
        {
            public uint dwFileAttributes;
            public FILETIME ftCreationTime;
            public FILETIME ftLastAccessTime;
            public FILETIME ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
            public uint dwReserved0;
            public uint dwReserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            public string cAlternateFileName;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct FILETIME
        {
            public uint dwLowDateTime;
            public uint dwHighDateTime;
        }

        private static readonly OperatingSystem osVersion = Environment.OSVersion;
        private static readonly Version Win7OSVersion = new Version(6, 1);

        private static string GetPermissionPath(string path, bool searchSubDirectories)
        {
            char end = path[path.Length - 1];

            if (!searchSubDirectories)
            {
                if (end == Path.DirectorySeparatorChar || end == Path.AltDirectorySeparatorChar)
                {
                    return path + ".";
                }

                return path + Path.DirectorySeparatorChar + "."; // Demand permission for the current directory only
            }
            
            if (end == Path.DirectorySeparatorChar || end == Path.AltDirectorySeparatorChar)
            {
                return path;
            }

            return path + Path.DirectorySeparatorChar; // Demand permission for the current directory and all subdirectories.
        }

        internal static IEnumerable<string> EnumerateFiles(string path, string[] fileExtensions, bool searchSubDirectories)
        {
            // Adapted from: http://weblogs.asp.net/podwysocki/archive/2008/10/16/functional-net-fighting-friction-in-the-bcl-with-directory-getfiles.aspx

            WIN32_FIND_DATAW findData = new WIN32_FIND_DATAW();

            FindExInfoLevels infoLevel = FindExInfoLevels.Standard;
            FindExAdditionalFlags flags = FindExAdditionalFlags.None;

            if (osVersion.Platform == PlatformID.Win32NT && osVersion.Version.CompareTo(Win7OSVersion) >= 0)
            {
                // Suppress the querying of short filenames and use a larger buffer on Windows 7 and later.
                infoLevel = FindExInfoLevels.Basic;
                flags = FindExAdditionalFlags.LargeFetch;
            }

            string fullPath = Path.GetFullPath(path);

            new FileIOPermission(FileIOPermissionAccess.PathDiscovery, GetPermissionPath(fullPath, searchSubDirectories)).Demand();

            Queue<string> directories = new Queue<string>();
            directories.Enqueue(fullPath);

            while (directories.Count > 0)
            {
                string currentPath = directories.Dequeue();

                using (SafeFindHandle findHandle = UnsafeNativeMethods.FindFirstFileExW(Path.Combine(currentPath, "*"), infoLevel, out findData, FindExSearchOps.NameMatch, IntPtr.Zero, flags))
                {
                    if (!findHandle.IsInvalid)
                    {
                        do
                        {
                            if ((findData.dwFileAttributes & NativeConstants.FILE_ATTRIBUTE_DIRECTORY) == NativeConstants.FILE_ATTRIBUTE_DIRECTORY)
                            {
                                if (searchSubDirectories)
                                {
                                    if (findData.cFileName != "." && findData.cFileName != ".." && (findData.dwFileAttributes & NativeConstants.FILE_ATTRIBUTE_REPARSE_POINT) == 0)
                                    {
                                        string subdirectory = Path.Combine(currentPath, findData.cFileName);

                                        directories.Enqueue(subdirectory);
                                    }
                                }
                            }
                            else
                            {
                                for (int i = 0; i < fileExtensions.Length; i++)
                                {
                                    if (findData.cFileName.EndsWith(fileExtensions[i], StringComparison.OrdinalIgnoreCase))
                                    {
                                        yield return Path.Combine(currentPath, findData.cFileName);
                                    }
                                }
                            }

                        } while (UnsafeNativeMethods.FindNextFileW(findHandle, out findData));
                    }
                }
            }

        }

    }
}
