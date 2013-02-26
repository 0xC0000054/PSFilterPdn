/* This file is from http://www.codeproject.com/Articles/38959/A-Faster-Directory-Enumerator
 * it is distributed under the CPOL http://www.codeproject.com/info/cpol10.aspx
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.ConstrainedExecution;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using Microsoft.Win32.SafeHandles;

namespace PSFilterPdn
{
    /// <summary>
    /// Contains information about a file returned by the 
    /// <see cref="FastDirectoryEnumerator"/> class.
    /// </summary>
    [Serializable]
    internal class FileData
    {
        /// <summary>
        /// Attributes of the file.
        /// </summary>
        public readonly FileAttributes Attributes;

        public DateTime CreationTime
        {
            get { return this.CreationTimeUtc.ToLocalTime(); }
        }

        /// <summary>
        /// File creation time in UTC
        /// </summary>
        public readonly DateTime CreationTimeUtc;

        /// <summary>
        /// Gets the last access time in local time.
        /// </summary>
        public DateTime LastAccesTime
        {
            get { return this.LastAccessTimeUtc.ToLocalTime(); }
        }
        
        /// <summary>
        /// File last access time in UTC
        /// </summary>
        public readonly DateTime LastAccessTimeUtc;

        /// <summary>
        /// Gets the last access time in local time.
        /// </summary>
        public DateTime LastWriteTime
        {
            get { return this.LastWriteTimeUtc.ToLocalTime(); }
        }
        
        /// <summary>
        /// File last write time in UTC
        /// </summary>
        public readonly DateTime LastWriteTimeUtc;
        
        /// <summary>
        /// Size of the file in bytes
        /// </summary>
        public readonly long Size;

        /// <summary>
        /// Name of the file
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// Full path to the file.
        /// </summary>
        public readonly string Path;

        /// <summary>
        /// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
        /// </summary>
        /// <returns>
        /// A <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
        /// </returns>
        public override string ToString()
        {
            return this.Name;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileData"/> class.
        /// </summary>
        /// <param name="dir">The directory that the file is stored at</param>
        /// <param name="findData">WIN32_FIND_DATA structure that this
        /// object wraps.</param>
        internal FileData(string dir, WIN32_FIND_DATA findData) 
        {
            this.Attributes = (FileAttributes)findData.dwFileAttributes;


            this.CreationTimeUtc = ConvertDateTime(findData.ftCreationTime);

            this.LastAccessTimeUtc = ConvertDateTime(findData.ftLastAccessTime);

            this.LastWriteTimeUtc = ConvertDateTime(findData.ftLastWriteTime);

            this.Size = CombineHighLowInts(findData.nFileSizeHigh, findData.nFileSizeLow);

            this.Name = findData.cFileName;
            this.Path = System.IO.Path.Combine(dir, findData.cFileName);
        }

        private static long CombineHighLowInts(uint high, uint low)
        {
            return (((long)high) << 0x20) | low;
        }

        private static DateTime ConvertDateTime(FILETIME time)
        {
            long fileTime = CombineHighLowInts(time.dwHighDateTime, time.dwLowDateTime);
            return DateTime.FromFileTimeUtc(fileTime);
        }
    }

    [StructLayoutAttribute(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct WIN32_FIND_DATA
    {

        /// DWORD->unsigned int
        public uint dwFileAttributes;

        /// FILETIME->_FILETIME
        public FILETIME ftCreationTime;

        /// FILETIME->_FILETIME
        public FILETIME ftLastAccessTime;

        /// FILETIME->_FILETIME
        public FILETIME ftLastWriteTime;

        /// DWORD->unsigned int
        public uint nFileSizeHigh;

        /// DWORD->unsigned int
        public uint nFileSizeLow;

        /// DWORD->unsigned int
        public uint dwReserved0;

        /// DWORD->unsigned int
        public uint dwReserved1;

        /// WCHAR[260]
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string cFileName;

        /// WCHAR[14]
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
        public string cAlternateFileName;
    }

    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    internal struct FILETIME
    {

        /// DWORD->unsigned int
        public uint dwLowDateTime;

        /// DWORD->unsigned int
        public uint dwHighDateTime;
    }

    /// <summary>
    /// A fast enumerator of files in a directory.  Use this if you need to get attributes for 
    /// all files in a directory.
    /// </summary>
    /// <remarks>
    /// This enumerator is substantially faster than using <see cref="Directory.GetFiles(string)"/>
    /// and then creating a new FileInfo object for each path.  Use this version when you 
    /// will need to look at the attributes of each file returned (for example, you need
    /// to check each file in a directory to see if it was modified after a specific date).
    /// </remarks>
    internal static class FastDirectoryEnumerator
    {
        /// <summary>
        /// Gets <see cref="FileData"/> for all the files in a directory.
        /// </summary>
        /// <param name="path">The path to search.</param>
        /// <returns>An object that implements <see cref="IEnumerable{FileData}"/> and 
        /// allows you to enumerate the files in the given directory.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="path"/> is a null reference (Nothing in VB)
        /// </exception>
        public static IEnumerable<FileData> EnumerateFiles(string path)
        {
            return FastDirectoryEnumerator.EnumerateFiles(path, "*");
        }

        /// <summary>
        /// Gets <see cref="FileData"/> for all the files in a directory that match a 
        /// specific filter.
        /// </summary>
        /// <param name="path">The path to search.</param>
        /// <param name="searchPattern">The search string to match against files in the path.</param>
        /// <returns>An object that implements <see cref="IEnumerable{FileData}"/> and 
        /// allows you to enumerate the files in the given directory.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="path"/> is a null reference (Nothing in VB)
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="searchPattern"/> is a null reference (Nothing in VB)
        /// </exception>
        public static IEnumerable<FileData> EnumerateFiles(string path, string searchPattern)
        {
            return FastDirectoryEnumerator.EnumerateFiles(path, searchPattern, SearchOption.TopDirectoryOnly);
        }

        /// <summary>
        /// Gets <see cref="FileData"/> for all the files in a directory that 
        /// match a specific filter, optionally including all sub directories.
        /// </summary>
        /// <param name="path">The path to search.</param>
        /// <param name="searchPattern">The search string to match against files in the path.</param>
        /// <param name="searchOption">
        /// One of the SearchOption values that specifies whether the search 
        /// operation should include all subdirectories or only the current directory.
        /// </param>
        /// <returns>An object that implements <see cref="IEnumerable{FileData}"/> and 
        /// allows you to enumerate the files in the given directory.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="path"/> is a null reference (Nothing in VB)
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="searchPattern"/> is a null reference (Nothing in VB)
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="searchOption"/> is not one of the valid values of the
        /// <see cref="System.IO.SearchOption"/> enumeration.
        /// </exception>
        public static IEnumerable<FileData> EnumerateFiles(string path, string searchPattern, SearchOption searchOption)
        {
            if (path == null)
            {
                throw new ArgumentNullException("path");
            }
            if (searchPattern == null)
            {
                throw new ArgumentNullException("searchPattern");
            }
            if ((searchOption != SearchOption.TopDirectoryOnly) && (searchOption != SearchOption.AllDirectories))
            {
                throw new ArgumentOutOfRangeException("searchOption");
            }

            string fullPath = Path.GetFullPath(path);

            return new FileEnumerable(fullPath, searchPattern, searchOption);
        }

        /// <summary>
        /// Gets <see cref="FileData"/> for all the files in a directory that match a 
        /// specific filter.
        /// </summary>
        /// <param name="path">The path to search.</param>
        /// <param name="searchPattern">The search string to match against files in the path.</param>
        /// <param name="searchOption">       
        /// One of the SearchOption values that specifies whether the search 
        /// operation should include all subdirectories or only the current directory.</param>
        /// <returns>An object that implements <see cref="IEnumerable{FileData}"/> and 
        /// allows you to enumerate the files in the given directory.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="path"/> is a null reference (Nothing in VB)
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="searchPattern"/> is a null reference (Nothing in VB)
        /// </exception>
        public static FileData[] GetFiles(string path, string searchPattern, SearchOption searchOption)
        {
            IEnumerable<FileData> e = FastDirectoryEnumerator.EnumerateFiles(path, searchPattern, searchOption);
            List<FileData> list = new List<FileData>(e);

            FileData[] retval = new FileData[list.Count];
            list.CopyTo(retval);

            return retval;
        }

        /// <summary>
        /// Provides the implementation of the 
        /// <see cref="T:System.Collections.Generic.IEnumerable`1"/> interface
        /// </summary>
        private class FileEnumerable : IEnumerable<FileData>
        {
            private readonly string m_path;
            private readonly string m_filter;
            private readonly SearchOption m_searchOption;

            /// <summary>
            /// Initializes a new instance of the <see cref="FileEnumerable"/> class.
            /// </summary>
            /// <param name="path">The path to search.</param>
            /// <param name="filter">The search string to match against files in the path.</param>
            /// <param name="searchOption">
            /// One of the SearchOption values that specifies whether the search 
            /// operation should include all subdirectories or only the current directory.
            /// </param>
            public FileEnumerable(string path, string filter, SearchOption searchOption)
            {
                m_path = path;
                m_filter = filter;
                m_searchOption = searchOption;
            }

            #region IEnumerable<FileData> Members

            /// <summary>
            /// Returns an enumerator that iterates through the collection.
            /// </summary>
            /// <returns>
            /// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can 
            /// be used to iterate through the collection.
            /// </returns>
            public IEnumerator<FileData> GetEnumerator()
            {
                return new FileEnumerator(m_path, m_filter, m_searchOption);
            }

            #endregion

            #region IEnumerable Members

            /// <summary>
            /// Returns an enumerator that iterates through a collection.
            /// </summary>
            /// <returns>
            /// An <see cref="T:System.Collections.IEnumerator"/> object that can be 
            /// used to iterate through the collection.
            /// </returns>
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
            {
                return new FileEnumerator(m_path, m_filter, m_searchOption);
            }

            #endregion
        }
        
        [System.Security.SuppressUnmanagedCodeSecurity]
        private static class UnsafeNativeMethods
        {
            [ReliabilityContract(Consistency.WillNotCorruptState, Cer.Success)]
            [DllImport("kernel32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool FindClose(IntPtr handle);

            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
            internal static extern SafeFindHandle FindFirstFileW([In] string fileName, out WIN32_FIND_DATA data);

            [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool FindNextFileW([In] SafeFindHandle hndFindFile, out WIN32_FIND_DATA lpFindFileData);
        }

        /// <summary>
        /// Wraps a FindFirstFile handle.
        /// </summary>
        private sealed class SafeFindHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="SafeFindHandle"/> class.
            /// </summary>
            [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
            internal SafeFindHandle()
                : base(true)
            {
            }

            /// <summary>
            /// When overridden in a derived class, executes the code required to free the handle.
            /// </summary>
            /// <returns>
            /// true if the handle is released successfully; otherwise, in the 
            /// event of a catastrophic failure, false. In this case, it 
            /// generates a releaseHandleFailed MDA Managed Debugging Assistant.
            /// </returns>
            protected override bool ReleaseHandle()
            {
                return UnsafeNativeMethods.FindClose(base.handle);
            }
        }

        /// <summary>
        /// Provides the implementation of the 
        /// <see cref="T:System.Collections.Generic.IEnumerator`1"/> interface
        /// </summary>
        private class FileEnumerator : IEnumerator<FileData>
        {

            /// <summary>
            /// Hold context information about where we current are in the directory search.
            /// </summary>
            private class SearchContext
            {
                public readonly string Path;
                public Queue<string> SubdirectoriesToProcess;

                public SearchContext(string path)
                {
                    this.Path = path;
                }
            }

            private string m_path;
            private string m_filter;
            private SearchOption m_searchOption;
            private Queue<SearchContext> m_contextStack;
            private SearchContext m_currentContext;

            private SafeFindHandle m_hndFindFile;
            private WIN32_FIND_DATA m_win_find_data = new WIN32_FIND_DATA();

            /// <summary>
            /// Initializes a new instance of the <see cref="FileEnumerator"/> class.
            /// </summary>
            /// <param name="path">The path to search.</param>
            /// <param name="filter">The search string to match against files in the path.</param>
            /// <param name="searchOption">
            /// One of the SearchOption values that specifies whether the search 
            /// operation should include all subdirectories or only the current directory.
            /// </param>
            public FileEnumerator(string path, string filter, SearchOption searchOption)
            {
                m_path = path;
                m_filter = filter;
                m_searchOption = searchOption;
                m_currentContext = new SearchContext(path);
                
                if (m_searchOption == SearchOption.AllDirectories)
                {
                    m_contextStack = new Queue<SearchContext>();
                }
            }

            #region IEnumerator<FileData> Members

            /// <summary>
            /// Gets the element in the collection at the current position of the enumerator.
            /// </summary>
            /// <value></value>
            /// <returns>
            /// The element in the collection at the current position of the enumerator.
            /// </returns>
            public FileData Current
            {
                get { return new FileData(m_path, m_win_find_data); }
            }

            #endregion

            #region IDisposable Members

            /// <summary>
            /// Performs application-defined tasks associated with freeing, releasing, 
            /// or resetting unmanaged resources.
            /// </summary>
            public void Dispose()
            {
                if (m_hndFindFile != null)
                {
                    m_hndFindFile.Dispose();
                }
            }

            #endregion

            #region IEnumerator Members

            /// <summary>
            /// Gets the element in the collection at the current position of the enumerator.
            /// </summary>
            /// <value></value>
            /// <returns>
            /// The element in the collection at the current position of the enumerator.
            /// </returns>
            object System.Collections.IEnumerator.Current
            {
                get { return new FileData(m_path, m_win_find_data); }
            }

            /// <summary>
            /// Advances the enumerator to the next element of the collection.
            /// </summary>
            /// <returns>
            /// true if the enumerator was successfully advanced to the next element; 
            /// false if the enumerator has passed the end of the collection.
            /// </returns>
            /// <exception cref="T:System.InvalidOperationException">
            /// The collection was modified after the enumerator was created.
            /// </exception>
            public bool MoveNext()
            {
                bool retval = false;

                //If the handle is null, this is first call to MoveNext in the current 
                // directory.  In that case, start a new search.
                if (m_currentContext.SubdirectoriesToProcess == null)
                {
                    if (m_hndFindFile == null)
                    {
                        new FileIOPermission(FileIOPermissionAccess.PathDiscovery, m_path).Demand();

                        string searchPath = Path.Combine(m_path, m_filter);
                        m_hndFindFile = UnsafeNativeMethods.FindFirstFileW(searchPath, out m_win_find_data);
                        retval = !m_hndFindFile.IsInvalid;
                    }
                    else
                    {
                        //Otherwise, find the next item.
                        retval =  UnsafeNativeMethods.FindNextFileW(m_hndFindFile, out m_win_find_data);
                    }
                }

                //If the call to FindNextFile or FindFirstFile succeeded...
                if (retval)
                {
                    if ((m_win_find_data.dwFileAttributes & 16) == 16)
                    {
                        //Ignore folders for now.   We call MoveNext recursively here to 
                        // move to the next item that FindNextFile will return.
                        return MoveNext();
                    }
                }
                else if (m_searchOption == SearchOption.AllDirectories)
                {
                    //SearchContext context = new SearchContext(m_hndFindFile, m_path);
                    //m_contextStack.Push(context);
                    //m_path = Path.Combine(m_path, m_win_find_data.cFileName);
                    //m_hndFindFile = null;

                    if (m_currentContext.SubdirectoriesToProcess == null)
                    {
                        string[] subDirectories = Directory.GetDirectories(m_path);
                        m_currentContext.SubdirectoriesToProcess = new Queue<string>(subDirectories);
                    }

                    if (m_currentContext.SubdirectoriesToProcess.Count > 0)
                    {
                        string subDir = m_currentContext.SubdirectoriesToProcess.Dequeue();

                        m_contextStack.Enqueue(m_currentContext);
                        m_path = subDir;
                        m_hndFindFile = null;
                        m_currentContext = new SearchContext(m_path);
                        return MoveNext();
                    }

                    //If there are no more files in this directory and we are 
                    // in a sub directory, pop back up to the parent directory and
                    // continue the search from there.
                    if (m_contextStack.Count > 0)
                    {
                        m_currentContext = m_contextStack.Dequeue();
                        m_path = m_currentContext.Path;
                        if (m_hndFindFile != null)
                        {
                            m_hndFindFile.Close();
                            m_hndFindFile = null;
                        }

                        return MoveNext();
                    }
                }

                return retval;
            }

            /// <summary>
            /// Sets the enumerator to its initial position, which is before the first element in the collection.
            /// </summary>
            /// <exception cref="T:System.InvalidOperationException">
            /// The collection was modified after the enumerator was created.
            /// </exception>
            public void Reset()
            {
                m_hndFindFile = null;
            }

            #endregion
        }
    }
}
