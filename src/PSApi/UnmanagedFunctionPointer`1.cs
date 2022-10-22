using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    /// <summary>
    /// A type-safe wrapper for <see cref="Marshal.GetFunctionPointerForDelegate(Delegate)"/>
    /// </summary>
    /// <typeparam name="TDelegate">The type of the delegate.</typeparam>
    [DebuggerDisplay("{pointer}")]
    [StructLayout(LayoutKind.Sequential)]
    internal readonly struct UnmanagedFunctionPointer<TDelegate> where TDelegate : Delegate
    {
        private readonly IntPtr pointer;

        public UnmanagedFunctionPointer(TDelegate d) => pointer = Marshal.GetFunctionPointerForDelegate(d);
    }
}
