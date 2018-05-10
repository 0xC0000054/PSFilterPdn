/////////////////////////////////////////////////////////////////////////////////
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Permissions;

namespace PSFilterPdn
{
    /// <summary>
    /// Enumerates through a directory using the native API.
    /// </summary>
    internal sealed class FileEnumerator : IEnumerator<string>
    {
        private sealed class SearchData
        {
            public readonly string path;
            public readonly bool isShortcut;

            /// <summary>
            /// Initializes a new instance of the <see cref="SearchData"/> class.
            /// </summary>
            /// <param name="path">The path.</param>
            /// <param name="isShortcut"><c>true</c> if the path is the target of a shortcut; otherwise, <c>false</c>.</param>
            /// <exception cref="ArgumentNullException"><paramref name="path"/> is null.</exception>
            public SearchData(string path, bool isShortcut)
            {
                if (path == null)
                {
                    throw new ArgumentNullException(nameof(path));
                }

                this.path = path;
                this.isShortcut = isShortcut;
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="SearchData"/> class with a sub directory of the parent <c>SearchData</c>.
            /// </summary>
            /// <param name="parent">The SearchData containing the current path.</param>
            /// <param name="subDirectoryName">The name of the sub directory within the path of the parent SearchData.</param>
            /// <exception cref="ArgumentNullException">
            /// <paramref name="parent"/> is null.
            /// or
            /// <paramref name="subDirectoryName"/> is null.
            /// </exception>
            public SearchData(SearchData parent, string subDirectoryName)
            {
                if (parent == null)
                {
                    throw new ArgumentNullException(nameof(parent));
                }
                if (subDirectoryName == null)
                {
                    throw new ArgumentNullException(nameof(subDirectoryName));
                }

                path = Path.Combine(parent.path, subDirectoryName);
                isShortcut = parent.isShortcut;
            }
        }

        /// <summary>
        /// Gets the demand path for the FileIOPermission.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="includeSubDirectories">if set to <c>true</c> include the sub directories of <paramref name="path"/>.</param>
        /// <returns></returns>
        private static string GetPermissionPath(string path, bool includeSubDirectories)
        {
            char end = path[path.Length - 1];

            if (!includeSubDirectories)
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

        /// <summary>
        /// Performs a FileIOPermission demand for PathDiscovery on the specified directory.
        /// </summary>
        /// <param name="directory">The path.</param>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        private static void DoDemand(string directory)
        {
            string demandPath = GetPermissionPath(directory, false);
            new FileIOPermission(FileIOPermissionAccess.PathDiscovery, demandPath).Demand();
        }

        private static uint SetErrorModeWrapper(uint newMode)
        {
            uint oldMode;

            if (OS.IsWindows7OrLater)
            {
                UnsafeNativeMethods.SetThreadErrorMode(newMode, out oldMode);
            }
            else
            {
                oldMode = UnsafeNativeMethods.SetErrorMode(newMode);
            }

            return oldMode;
        }

        private const int STATE_INIT = 0;
        private const int STATE_FIND_FILES = 1;
        private const int STATE_FINISH = 2;

        private SafeFindHandle handle;
        private ShellLink shellLink;
        private Queue<SearchData> searchDirectories;
        private SearchData searchData;
        private string current;
        private int state;
        private bool disposed;
        private bool needPathDiscoveryDemand;
        private string shellLinkTarget;

        private readonly NativeEnums.FindExInfoLevel infoLevel;
        private readonly NativeEnums.FindExAdditionalFlags additionalFlags;
        private readonly string fileExtension;
        private readonly SearchOption searchOption;
        private readonly bool dereferenceLinks;
        private readonly uint oldErrorMode;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileEnumerator"/> class.
        /// </summary>
        /// <param name="path">The directory to search.</param>
        /// <param name="fileExtension">The file extension to search for.</param>
        /// <param name="searchOption">
        /// One of the <see cref="SearchOption"/> values that specifies whether the search operation should include
        /// only the current directory or should include all subdirectories.
        /// </param>
        /// <param name="dereferenceLinks">if set to <c>true</c> search the target of shortcuts.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="path"/> in null.
        /// -or-
        /// <paramref name="fileExtension"/> is null.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="searchOption"/> is not a valid <see cref="SearchOption"/> value.</exception>
        /// <exception cref="PathTooLongException">The specified path, file name, or combined exceed the system-defined maximum length.</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        public FileEnumerator(string path, string fileExtension, SearchOption searchOption, bool dereferenceLinks)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (fileExtension == null)
            {
                throw new ArgumentNullException(nameof(fileExtension));
            }
            if (searchOption != SearchOption.AllDirectories && searchOption != SearchOption.TopDirectoryOnly)
            {
                throw new ArgumentOutOfRangeException(nameof(searchOption));
            }

            string fullPath = Path.GetFullPath(path);
            string demandPath = GetPermissionPath(fullPath, false);
            new FileIOPermission(FileIOPermissionAccess.PathDiscovery, demandPath).Demand();
            needPathDiscoveryDemand = false;

            searchData = new SearchData(fullPath, false);
            this.fileExtension = fileExtension;
            this.searchOption = searchOption;
            searchDirectories = new Queue<SearchData>();
            if (dereferenceLinks)
            {
                shellLink = new ShellLink();
                this.dereferenceLinks = true;
            }
            else
            {
                shellLink = null;
                this.dereferenceLinks = false;
            }
            shellLinkTarget = null;

            if (OS.IsWindows7OrLater)
            {
                // Suppress the querying of short filenames and use a larger buffer on Windows 7 and later.
                infoLevel = NativeEnums.FindExInfoLevel.Basic;
                additionalFlags = NativeEnums.FindExAdditionalFlags.LargeFetch;
            }
            else
            {
                infoLevel = NativeEnums.FindExInfoLevel.Standard;
                additionalFlags = NativeEnums.FindExAdditionalFlags.None;
            }
            oldErrorMode = SetErrorModeWrapper(NativeConstants.SEM_FAILCRITICALERRORS);
            state = -1;
            current = null;
            disposed = false;
            Init();
        }

        private void Init()
        {
            WIN32_FIND_DATAW findData = new WIN32_FIND_DATAW();
            string searchPath = Path.Combine(searchData.path, "*");
            handle = UnsafeNativeMethods.FindFirstFileExW(
                searchPath,
                infoLevel,
                findData,
                NativeEnums.FindExSearchOp.NameMatch,
                IntPtr.Zero,
                additionalFlags);

            if (handle.IsInvalid)
            {
                handle.Dispose();
                handle = null;

                state = STATE_FINISH;
            }
            else
            {
                state = STATE_INIT;
                if (FirstFileIncluded(findData))
                {
                    current = CreateFilePath(findData);
                }
            }
        }

        private bool FileMatchesFilter(string file)
        {
            return file.EndsWith(fileExtension, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Resolves the shortcut target.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="isDirectory">set to <c>true</c> if the target is a directory.</param>
        /// <returns>The target of the shortcut; or null if the target does not exist.</returns>
        private static string ResolveShortcutTarget(string path, out bool isDirectory)
        {
            isDirectory = false;

            if (!string.IsNullOrEmpty(path))
            {
                uint attributes = UnsafeNativeMethods.GetFileAttributesW(path);
                if (attributes != NativeConstants.INVALID_FILE_ATTRIBUTES)
                {
                    isDirectory = (attributes & NativeConstants.FILE_ATTRIBUTE_DIRECTORY) == NativeConstants.FILE_ATTRIBUTE_DIRECTORY;
                    return path;
                }
            }

            return null;
        }

        private bool FirstFileIncluded(WIN32_FIND_DATAW findData)
        {
            if ((findData.dwFileAttributes & NativeConstants.FILE_ATTRIBUTE_DIRECTORY) == NativeConstants.FILE_ATTRIBUTE_DIRECTORY)
            {
                if (searchOption == SearchOption.AllDirectories && !findData.cFileName.Equals(".") && !findData.cFileName.Equals(".."))
                {
                    searchDirectories.Enqueue(new SearchData(searchData, findData.cFileName));
                }
            }
            else
            {
                return IsFileIncluded(findData);
            }

            return false;
        }

        private bool IsFileIncluded(WIN32_FIND_DATAW findData)
        {
            if (findData.cFileName.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase) && dereferenceLinks)
            {
                // Do not search shortcuts recursively.
                if (!searchData.isShortcut && shellLink.Load(Path.Combine(searchData.path, findData.cFileName)))
                {
                    bool isDirectory;
                    string target = ResolveShortcutTarget(shellLink.Path, out isDirectory);

                    if (!string.IsNullOrEmpty(target))
                    {
                        if (isDirectory)
                        {
                            // If the shortcut target is a directory, add it to the search list.
                            searchDirectories.Enqueue(new SearchData(target, true));
                        }
                        else if (FileMatchesFilter(target))
                        {
                            shellLinkTarget = target;
                            return true;
                        }
                    }
                }
            }
            else if (FileMatchesFilter(findData.cFileName))
            {
                if (needPathDiscoveryDemand)
                {
                    DoDemand(searchData.path);
                    needPathDiscoveryDemand = false;
                }
                shellLinkTarget = null;

                return true;
            }

            return false;
        }

        private string CreateFilePath(WIN32_FIND_DATAW findData)
        {
            if (shellLinkTarget != null)
            {
                return shellLinkTarget;
            }
            else
            {
                return Path.Combine(searchData.path, findData.cFileName);
            }
        }

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        public string Current
        {
            get
            {
                return current;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;

                if (handle != null)
                {
                    handle.Dispose();
                    handle = null;
                }

                if (shellLink != null)
                {
                    shellLink.Dispose();
                    shellLink = null;
                }
                current = null;
                state = -1;
                SetErrorModeWrapper(oldErrorMode);
            }
        }

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        object IEnumerator.Current
        {
            get
            {
                if (current == null)
                {
                    throw new InvalidOperationException();
                }

                return current;
            }
        }

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>
        /// true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.
        /// </returns>
        public bool MoveNext()
        {
            WIN32_FIND_DATAW findData = new WIN32_FIND_DATAW();

            switch (state)
            {
                case STATE_INIT:
                    state = STATE_FIND_FILES;

                    if (current != null)
                    {
                        return true;
                    }
                    else
                    {
                        goto case STATE_FIND_FILES;
                    }
                case STATE_FIND_FILES:
                    do
                    {
                        if (handle == null)
                        {
                            searchData = searchDirectories.Dequeue();

                            string searchPath = Path.Combine(searchData.path, "*");
                            handle = UnsafeNativeMethods.FindFirstFileExW(
                                searchPath,
                                infoLevel,
                                findData,
                                NativeEnums.FindExSearchOp.NameMatch,
                                IntPtr.Zero,
                                additionalFlags);

                            if (handle.IsInvalid)
                            {
                                handle.Dispose();
                                handle = null;

                                if (searchDirectories.Count > 0)
                                {
                                    continue;
                                }
                                else
                                {
                                    state = STATE_FINISH;
                                    goto case STATE_FINISH;
                                }
                            }

                            needPathDiscoveryDemand = true;
                            if (FirstFileIncluded(findData))
                            {
                                current = CreateFilePath(findData);
                                return true;
                            }
                        }

                        while (UnsafeNativeMethods.FindNextFileW(handle, findData))
                        {
                            if ((findData.dwFileAttributes & NativeConstants.FILE_ATTRIBUTE_DIRECTORY) == 0)
                            {
                                if (IsFileIncluded(findData))
                                {
                                    current = CreateFilePath(findData);
                                    return true;
                                }
                            }
                            else if (searchOption == SearchOption.AllDirectories && !findData.cFileName.Equals(".") && !findData.cFileName.Equals(".."))
                            {
                                searchDirectories.Enqueue(new SearchData(searchData, findData.cFileName));
                            }
                        }

                        handle.Dispose();
                        handle = null;

                    } while (searchDirectories.Count > 0);

                    state = STATE_FINISH;
                    goto case STATE_FINISH;
                case STATE_FINISH:
                    Dispose();
                    break;
            }

            return false;
        }

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first element in the collection.
        /// </summary>
        void IEnumerator.Reset()
        {
            throw new NotSupportedException();
        }
    }
}
