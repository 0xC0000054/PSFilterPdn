/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
//   Copyright (C) 2010-2023 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using PaintDotNet;
using TerraFX.Interop.Windows;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

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
                HRESULT hr = Windows.CoCreateInstance(Windows.__uuidof<TerraFX.Interop.Windows.ShellLink>(),
                                                      null,
                                                      (uint)CLSCTX.CLSCTX_INPROC_SERVER,
                                                      Windows.__uuidof<IShellLinkW>(),
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
        /// <returns>
        /// <see langword="true"/> if the shortcut target path was retrieved; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentNullException"><paramref name="path"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="path"/> is empty.</exception>
        public bool TryGetTargetPath(string path, [MaybeNullWhen(false)] out string targetPath)
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
                        hr = asPersistFile.Get()->Load((ushort*)pszFileName, 0);
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

                hr = shellLink.Get()->GetPath((ushort*)pszFile, cchMaxPath, null, 0U);

                if (hr.SUCCEEDED)
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                shellLink.Dispose();
            }
        }
    }
}
