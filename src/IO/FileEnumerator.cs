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
using TerraFX.Interop.Windows;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Enumeration;

namespace PSFilterPdn
{
    /// <summary>
    /// Enumerates through the files in a directory, optionally dereferencing shortcuts.
    /// </summary>
    internal sealed class FileEnumerator : Disposable, IEnumerator<string>
    {
        private const int STATE_INIT = 0;
        private const int STATE_FIND_NEXT_FILE = 1;
        private const int STATE_SEARCH_NEXT_DIRECTORY = 2;
        private const int STATE_FINISH = 3;

        private int state;
        private FileExtensionEnumerator? fileEnumerator;
        private ShellLink? shellLink;
        private string? shellLinkTarget;
        private string? current;
        private readonly string fileExtension;
        private readonly EnumerationOptions enumerationOptions;
        private readonly bool dereferenceLinks;
        private readonly Queue<string> searchDirectories;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileEnumerator"/> class.
        /// </summary>
        /// <param name="path">The directory to search.</param>
        /// <param name="fileExtension">The file extension to search for.</param>
        /// <param name="recurseSubdirectories">
        /// <see langword="true"/> if the subdirectories of <paramref name="path"/> should be included
        /// in the search; otherwise, <see langword="false"/>.
        /// </param>
        /// <param name="dereferenceLinks">
        /// <see langword="true"/> if shortcut targets should be included in the search; otherwise, <see langword="false"/>.</param>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="path"/> in null.
        /// -or-
        /// <paramref name="fileExtension"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// <paramref name="path"/> is a 0 length string, or contains only white-space,
        /// or contains one or more invalid characters as defined by <see cref="Path.GetInvalidPathChars"/>.</exception>
        /// <exception cref="DirectoryNotFoundException">The directory specified by <paramref name="path"/> does not exist.</exception>
        /// <exception cref="IOException"><paramref name="path"/> is a file.</exception>
        /// <exception cref="PathTooLongException">
        /// The specified path, file name, or combined exceed the system-defined maximum length.
        /// For example, on Windows-based platforms, paths must be less than 248 characters and file names must be less than 260 characters.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">The caller does not have the required permission.</exception>
        /// <exception cref="SecurityException">The caller does not have the required permission.</exception>
        public FileEnumerator(string path, string fileExtension, bool recurseSubdirectories, bool dereferenceLinks)
        {
            ArgumentNullException.ThrowIfNull(path, nameof(path));
            ArgumentNullException.ThrowIfNull(fileExtension, nameof(fileExtension));

            this.fileExtension = fileExtension;
            enumerationOptions = new EnumerationOptions
            {
                RecurseSubdirectories = recurseSubdirectories
            };
            searchDirectories = new Queue<string>();
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
            current = null;

            try
            {
                fileEnumerator = CreateFileEnumerator(path);
                state = STATE_INIT;
            }
            catch (Exception)
            {
                if (shellLink is not null)
                {
                    shellLink.Dispose();
                    shellLink = null;
                }
                throw;
            }
        }

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        public string Current => current!;

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        object IEnumerator.Current
        {
            get
            {
                if (Current is null)
                {
                    throw new InvalidOperationException();
                }

                return Current;
            }
        }

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first element in the collection.
        /// </summary>
        void IEnumerator.Reset() => throw new NotSupportedException();

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>
        /// true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.
        /// </returns>
        public bool MoveNext()
        {
            switch (state)
            {
                case STATE_INIT:
                    if (fileEnumerator!.MoveNext())
                    {
                        state = STATE_FIND_NEXT_FILE;

                        if (IsFileIncluded(fileEnumerator.Current))
                        {
                            current = CreateFilePath(fileEnumerator.Current);
                            return true;
                        }
                        else
                        {
                            goto case STATE_FIND_NEXT_FILE;
                        }
                    }
                    else
                    {
                        state = STATE_FINISH;
                        goto case STATE_FINISH;
                    }
                case STATE_FIND_NEXT_FILE:
                    while (fileEnumerator!.MoveNext())
                    {
                        if (IsFileIncluded(fileEnumerator.Current))
                        {
                            current = CreateFilePath(fileEnumerator.Current);
                            return true;
                        }
                    }

                    if (searchDirectories.Count > 0)
                    {
                        state = STATE_SEARCH_NEXT_DIRECTORY;
                        goto case STATE_SEARCH_NEXT_DIRECTORY;
                    }
                    else
                    {
                        state = STATE_FINISH;
                        goto case STATE_FINISH;
                    }
                case STATE_SEARCH_NEXT_DIRECTORY:
                    while (searchDirectories.Count > 0)
                    {
                        fileEnumerator?.Dispose();

                        // Additional search directories can be added if one of the folders that are being
                        // searched contains a shortcut to a directory.
                        fileEnumerator = CreateFileEnumerator(searchDirectories.Dequeue());

                        if (fileEnumerator.MoveNext())
                        {
                            state = STATE_FIND_NEXT_FILE;

                            if (IsFileIncluded(fileEnumerator.Current))
                            {
                                current = CreateFilePath(fileEnumerator.Current);
                                return true;
                            }
                            else
                            {
                                goto case STATE_FIND_NEXT_FILE;
                            }
                        }
                    }
                    state = STATE_FINISH;
                    goto case STATE_FINISH;
                case STATE_FINISH:
                    Dispose();
                    break;
            }

            return false;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                fileEnumerator?.Dispose();

                if (shellLink is not null)
                {
                    shellLink.Dispose();
                    shellLink = null;
                }

                current = null;
                state = -1;
            }
        }

        private FileExtensionEnumerator CreateFileEnumerator(string path)
            => new(path, enumerationOptions, fileExtension, dereferenceLinks);

        private string CreateFilePath(string path) => shellLinkTarget ?? path;

        private unsafe bool IsFileIncluded(string path)
        {
            bool result = false;

            if (path.EndsWith(fileExtension, StringComparison.OrdinalIgnoreCase))
            {
                shellLinkTarget = null;
                result = true;
            }
            else if (path.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase) && dereferenceLinks)
            {
                if (shellLink!.TryGetTargetPath(path, out string? target))
                {
                    if (!string.IsNullOrEmpty(target))
                    {
                        uint fileAttributes = Windows.INVALID_FILE_ATTRIBUTES;

                        fixed (char* lpFileName = target)
                        {
                            fileAttributes = Windows.GetFileAttributesW((ushort*)lpFileName);
                        }

                        if (fileAttributes != Windows.INVALID_FILE_ATTRIBUTES)
                        {
                            if ((fileAttributes & FILE.FILE_ATTRIBUTE_DIRECTORY) != 0)
                            {
                                // If the shortcut target is a directory, add it to the search list.
                                searchDirectories.Enqueue(target);
                            }
                            else if (target.EndsWith(fileExtension, StringComparison.OrdinalIgnoreCase))
                            {
                                shellLinkTarget = target;
                                result = true;
                            }
                        }
                    }
                }
            }

            return result;
        }

        private sealed class FileExtensionEnumerator : FileSystemEnumerator<string>
        {
            private readonly bool dereferenceLinks;
            private readonly string fileExtension;

            public FileExtensionEnumerator(string directory,
                                           EnumerationOptions options,
                                           string fileExtension,
                                           bool dereferenceLinks)
                : base(directory, options)
            {
                ArgumentNullException.ThrowIfNull(fileExtension, nameof(fileExtension));

                this.dereferenceLinks = dereferenceLinks;
                this.fileExtension = fileExtension;
            }

            // The previous code always silently ignored errors when enumerating the directories.
            protected override bool ContinueOnError(int error) => true;

            protected override bool ShouldIncludeEntry(ref FileSystemEntry entry)
            {
                bool result = false;

                if (!entry.IsDirectory)
                {
                    result = entry.FileName.EndsWith(fileExtension, StringComparison.OrdinalIgnoreCase)
                             || (dereferenceLinks && entry.FileName.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase));
                }

                return result;
            }

            protected override string TransformEntry(ref FileSystemEntry entry)
                => entry.ToFullPath();
        }
    }
}
