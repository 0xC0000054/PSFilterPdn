using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace PSFilterPdn
{
    /// <summary>
    /// Encapsulates a ShellLink shortcut file
    /// </summary>
    internal sealed class ShellLink : IDisposable
    {
        private ShellLinkCoClass shellLinkCoClass;
        private NativeInterfaces.IShellLinkW shellLink;

        private const int STGM_READ = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShellLink"/> class.
        /// </summary>
        public ShellLink ()
        {
            shellLinkCoClass = new ShellLinkCoClass();
            shellLink = (NativeInterfaces.IShellLinkW)shellLinkCoClass;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShellLink"/> class.
        /// </summary>
        /// <param name="linkPath">The link path to load.</param>
        public ShellLink(string linkPath)
        {
            shellLinkCoClass = new ShellLinkCoClass();
            shellLink = (NativeInterfaces.IShellLinkW)shellLinkCoClass;

            this.Load(linkPath);
        }

        /// <summary>
        /// Loads a shortcut from a file.
        /// </summary>
        /// <param name="linkPath">The shortcut to load.</param>
        public void Load(string linkPath)
        {
            ((NativeInterfaces.IPersistFile)shellLink).Load(linkPath, STGM_READ);
        }

        /// <summary>
        /// Gets the target path of the shortcut.
        /// </summary>
        public string Path
        {
            get
            {
                StringBuilder sb = new StringBuilder(260);

                shellLink.GetPath(sb, sb.MaxCapacity, IntPtr.Zero, 0);

                return sb.ToString();
            }
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
        private bool disposed;
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

                if (shellLinkCoClass != null)
                {
                    Marshal.ReleaseComObject(shellLinkCoClass);
                    shellLinkCoClass = null;
                } 
            }
        }
    }
}
