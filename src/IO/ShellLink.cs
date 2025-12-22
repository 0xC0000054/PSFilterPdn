/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
//   Copyright (C) 2010-2025 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using PaintDotNet;
using TerraFX.Interop.Windows;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

using static TerraFX.Interop.Windows.Windows;

namespace PSFilterPdn
{
    /// <summary>
    /// Encapsulates a ShellLink shortcut file
    /// </summary>
    internal sealed unsafe class ShellLink : Disposable
    {
        private ComPtr<IShellLinkW> shellLink;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShellLink"/> class.
        /// </summary>
        public ShellLink()
        {
            fixed (IShellLinkW** ppShellLink = shellLink)
            {
                HRESULT hr = CoCreateInstance(__uuidof<TerraFX.Interop.Windows.ShellLink>(),
                                              null,
                                              (uint)CLSCTX.CLSCTX_INPROC_SERVER,
                                               __uuidof<IShellLinkW>(),
                                              (void**)ppShellLink);
                if (hr.FAILED)
                {
                    Marshal.ThrowExceptionForHR(hr.Value);
                }
            }
        }

        /// <summary>
        /// Attempts to get the shortcut target path.
        /// </summary>
        /// <param name="path">The shortcut to load.</param>
        /// <param name="targetPath">The path of the shortcut target.</param>
        /// <param name="fileAttributes">The file attributes of the shortcut target.</param>
        /// <returns>
        /// <see langword="true"/> if the shortcut target path was retrieved; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is empty.</exception>
        /// <exception cref="ObjectDisposedException">The object has been disposed.</exception>
        public bool TryGetTargetPath(
            string path,
            [MaybeNullWhen(false)] out string targetPath,
            out uint fileAttributes)
        {
            ArgumentNullException.ThrowIfNull(path, nameof(path));

            if (path.Length == 0)
            {
                ExceptionUtil.ThrowArgumentException("Must not be empty.", nameof(path));
            }

            if (IsDisposed)
            {
                ExceptionUtil.ThrowObjectDisposedException(nameof(ShellLink));
            }

            HRESULT hr;

            using (ComPtr<IPersistFile> asPersistFile = default)
            {
                hr = shellLink.As(&asPersistFile);

                if (hr.SUCCEEDED)
                {
                    fixed (char* pszFileName = path)
                    {
                        hr = asPersistFile.Get()->Load(pszFileName, 0);
                    }
                }
            }

            if (hr.SUCCEEDED)
            {
                const int cchMaxPath = MAX.MAX_PATH;

                // We use stackalloc instead of a an ArrayPool because the IShellLinkW.GetPath method
                // does not provide the length of the native string.
                // The runtime will determine the length of the native string when it reads from the
                // allocated buffer.
                char* pszFile = stackalloc char[cchMaxPath];
                WIN32_FIND_DATAW findData;

                hr = shellLink.Get()->GetPath(pszFile, cchMaxPath, &findData, 0U);

                if (hr.SUCCEEDED)
                {
                    targetPath = new string(pszFile);
                    fileAttributes = findData.dwFileAttributes;
                    return true;
                }
            }

            targetPath = null;
            fileAttributes = 0;
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                shellLink.Dispose();
            }
        }
    }
}
