/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2022 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.IO;

namespace PSFilterPdn
{
    internal sealed class PSFilterShimDataFolder : IDisposable
    {
        private bool disposed;
        private readonly DirectoryInfo directoryInfo;

        public PSFilterShimDataFolder()
        {
            disposed = false;
            directoryInfo = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));
            directoryInfo.Create();
        }

        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;

                try
                {
                    directoryInfo.Delete(recursive: true);
                }
                catch (ArgumentException)
                {
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
        }

        public void Clean()
        {
            VerifyNotDisposed();

            // Remove all the existing files in the directory.
            foreach (FileInfo file in directoryInfo.EnumerateFiles())
            {
                file.Delete();
            }
        }

        public string GetRandomFilePathWithExtension(string extension)
        {
            VerifyNotDisposed();

            return Path.Combine(directoryInfo.FullName, GetFileNameWithExtension(extension));
        }

        private static string GetFileNameWithExtension(string extension)
        {
            string fileName;

            if (!string.IsNullOrEmpty(extension))
            {
                fileName = Path.ChangeExtension(Path.GetRandomFileName(), extension);
            }
            else
            {
                fileName = Path.GetFileNameWithoutExtension(Path.GetRandomFileName());
            }

            return fileName;
        }

        private void VerifyNotDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(PSFilterShimDataFolder));
            }
        }
    }
}
