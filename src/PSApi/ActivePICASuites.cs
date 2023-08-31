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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

namespace PSFilterLoad.PSApi
{
    internal sealed class ActivePICASuites : IDisposable
    {
        [DebuggerDisplay("{DebuggerDisplay,nq}")]
        internal sealed class PICASuiteKey : IEquatable<PICASuiteKey>
        {
            private readonly string name;
            private readonly int version;

            /// <summary>
            /// Initializes a new instance of the <see cref="PICASuiteKey"/> class.
            /// </summary>
            /// <param name="name">The suite name.</param>
            /// <param name="version">The suite version.</param>
            /// <exception cref="ArgumentNullException"><paramref name="name"/> is null.</exception>
            public PICASuiteKey(string name, int version)
            {
                if (name == null)
                {
                    throw new ArgumentNullException(nameof(name));
                }

                this.name = name;
                this.version = version;
            }

            /// <summary>
            /// Gets the suite name.
            /// </summary>
            /// <value>
            /// The suite name.
            /// </value>
            public string Name => name;

            /// <summary>
            /// Gets the suite version.
            /// </summary>
            /// <value>
            /// The suite version.
            /// </value>
            public int Version => version;

            private string DebuggerDisplay => string.Format(CultureInfo.CurrentCulture, "{0}, version {1}", name, version);

            public override bool Equals(object? obj)
            {
                if (obj == null)
                {
                    return false;
                }

                if (ReferenceEquals(this, obj))
                {
                    return true;
                }

                PICASuiteKey? key = obj as PICASuiteKey;
                if (key == null)
                {
                    return false;
                }

                return Equals(key);
            }

            public override int GetHashCode()
            {
                int hash = 23;

                unchecked
                {
                    hash = (hash * 127) + name.GetHashCode();
                    hash = (hash * 127) + version.GetHashCode();
                }

                return hash;
            }

            public bool Equals(PICASuiteKey? other)
            {
                if (other == null)
                {
                    return false;
                }

                return name.Equals(other.name, StringComparison.Ordinal) && version == other.version;
            }

            public static bool operator ==(PICASuiteKey? p1, PICASuiteKey? p2)
            {
                if (ReferenceEquals(p1, p2))
                {
                    return true;
                }

                if (((object?)p1) == null || ((object?)p2) == null)
                {
                    return false;
                }

                return p1.Equals(p2);
            }

            public static bool operator !=(PICASuiteKey? p1, PICASuiteKey? p2)
            {
                return !(p1 == p2);
            }
        }

        private sealed class PICASuite : IDisposable
        {
            private IntPtr suitePointer;
            private int refCount;
            private bool disposed;

            public IntPtr SuitePointer => suitePointer;

            public int RefCount
            {
                get => refCount;
                set => refCount = value;
            }

            public PICASuite(IntPtr suite)
            {
                suitePointer = suite;
                refCount = 1;
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            ~PICASuite()
            {
                Dispose(false);
            }

            private void Dispose(bool disposing)
            {
                if (!disposed)
                {
                    if (disposing)
                    {
                    }

                    if (suitePointer != IntPtr.Zero)
                    {
                        Memory.Free(suitePointer);
                        suitePointer = IntPtr.Zero;
                    }

                    disposed = true;
                }
            }
        }

        private Dictionary<PICASuiteKey, PICASuite> activeSuites;
        private bool disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ActivePICASuites"/> class.
        /// </summary>
        public ActivePICASuites()
        {
            activeSuites = new Dictionary<PICASuiteKey, PICASuite>();
            disposed = false;
        }

        /// <summary>
        /// Allocates a new PICA suite.
        /// </summary>
        /// <param name="key">The <see cref="PICASuiteKey"/> specifying the suite name and version.</param>
        /// <param name="suite">The suite to be marshaled to unmanaged memory.</param>
        /// <returns>The pointer to the allocated suite.</returns>
        public IntPtr AllocateSuite(PICASuiteKey key, IPICASuiteAllocator factory)
        {
            if (disposed)
            {
                throw new ObjectDisposedException("ActivePICASuites");
            }

            IntPtr suitePointer = factory.Allocate(key.Version);
            try
            {
                activeSuites.Add(key, new PICASuite(suitePointer));
            }
            catch (Exception)
            {
                Memory.Free(suitePointer);
                throw;
            }

            return suitePointer;
        }

        /// <summary>
        /// Determines whether the specified suite is loaded.
        /// </summary>
        /// <param name="key">The <see cref="PICASuiteKey"/> specifying the suite name and version.</param>
        /// <returns>
        ///   <c>true</c> if the specified suite is loaded; otherwise, <c>false</c>.
        /// </returns>
        public bool IsLoaded(PICASuiteKey key)
        {
            if (disposed)
            {
                throw new ObjectDisposedException("ActivePICASuites");
            }
            return activeSuites.ContainsKey(key);
        }

        /// <summary>
        /// Increments the reference count on the specified suite.
        /// </summary>
        /// <param name="key">The <see cref="PICASuiteKey"/> specifying the suite name and version.</param>
        /// <returns>The pointer to the suite instance.</returns>
        public IntPtr AddRef(PICASuiteKey key)
        {
            PICASuite suite = activeSuites[key];
            suite.RefCount += 1;
            activeSuites[key] = suite;

            return suite.SuitePointer;
        }

        /// <summary>
        /// Decrements the reference count and removes the specified suite if it is zero.
        /// </summary>
        /// <param name="key">The <see cref="PICASuiteKey"/> specifying the suite name and version.</param>
        public void Release(PICASuiteKey key)
        {
            if (disposed)
            {
                throw new ObjectDisposedException("ActivePICASuites");
            }

            if (activeSuites.TryGetValue(key, out PICASuite? suite))
            {
                suite.RefCount -= 1;

                if (suite.RefCount == 0)
                {
                    suite.Dispose();
                    activeSuites.Remove(key);
                }
                else
                {
                    activeSuites[key] = suite;
                }
            }
        }

        public void Dispose()
        {
            if (!disposed)
            {
                disposed = true;

                foreach (PICASuite item in activeSuites.Values)
                {
                    item.Dispose();
                }
                activeSuites.Clear();
            }
        }
    }
}
