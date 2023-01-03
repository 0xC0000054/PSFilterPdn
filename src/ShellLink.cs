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

using PaintDotNet;
using PSFilterPdn.Interop;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace PSFilterPdn
{
    /// <summary>
    /// Encapsulates a ShellLink shortcut file
    /// </summary>
    internal sealed class ShellLink : IDisposable
    {
        private NativeInterfaces.IShellLinkW shellLink;
        private bool disposed;

        [ComImport(), Guid(NativeConstants.CLSID_ShellLink)]
        private class ShellLinkCoClass
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShellLink"/> class.
        /// </summary>
        public ShellLink()
        {
            shellLink = (NativeInterfaces.IShellLinkW)new ShellLinkCoClass();
        }

        /// <summary>
        /// Attempts to get the shortcut target path.
        /// </summary>
        /// <param name="path">The shortcut to load.</param>
        /// <param name="targetPath">The path of the shortcut target.</param>
        /// <returns>
        /// <see langword="true"/> if the shortcut target path was retrieved; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is empty.</exception>
        public unsafe bool TryGetTargetPath(string path, [NotNullWhen(true)] out string targetPath)
        {
            ArgumentNullException.ThrowIfNull(path, nameof(path));

            if (path.Length == 0)
            {
                ExceptionUtil.ThrowArgumentException("Must not be empty.", nameof(path));
            }

            if (disposed)
            {
                ExceptionUtil.ThrowObjectDisposedException(nameof(ShellLink));
            }

            int hr;

            fixed (char* pszFileName = path)
            {
                hr = ((NativeInterfaces.IPersistFile)shellLink).Load((ushort*)pszFileName, NativeConstants.STGM_READ);
            }

            if (hr == NativeConstants.S_OK)
            {
                const int cchMaxPath = NativeConstants.MAX_PATH;

                // We use stackalloc instead of a an ArrayPool because the IShellLinkW.GetPath method
                // does not provide the length of the native string.
                // The runtime will determine the length of the native string when it reads from the
                // allocated buffer.
                char* pszFile = stackalloc char[cchMaxPath];

                hr = shellLink.GetPath((ushort*)pszFile, cchMaxPath, IntPtr.Zero, 0U);

                if (hr == NativeConstants.S_OK)
                {
                    targetPath = new string(pszFile);
                    return true;
                }
            }

            targetPath = null;
            return false;
        }

        /// <summary>
        /// Releases unmanaged resources and performs other cleanup operations before the
        /// <see cref="ShellLink"/> is reclaimed by garbage collection.
        /// </summary>
        ~ShellLink()
        {
            Dispose(false);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                disposed = true;

                if (disposing)
                {
                }

                if (shellLink != null)
                {
                    Marshal.ReleaseComObject(shellLink);
                    shellLink = null;
                }
            }
        }
    }
}
