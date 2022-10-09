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

namespace PSFilterLoad.PSApi
{
#if DEBUG
    internal static class DebugUtils
    {
        private static DebugFlags debugFlags = DebugFlags.None;

        internal static DebugFlags GlobalDebugFlags
        {
            get => debugFlags;
            set => debugFlags = value;
        }

        internal static void Ping(DebugFlags flag, string message)
        {
            if ((debugFlags & flag) == flag)
            {
                System.Diagnostics.StackFrame sf = new System.Diagnostics.StackFrame(1);
                string name = sf.GetMethod().Name;
                System.Diagnostics.Debug.WriteLine(string.Format("Function: {0}, {1}\r\n", name, message));
            }
        }

        internal static unsafe string PointerToString<T>(T* value) where T : unmanaged => value != null ? (*value).ToString() : "<null>";

        internal static string PropToString(uint prop)
        {
            byte[] bytes = System.BitConverter.GetBytes(prop);
            return new string(new char[] { (char)bytes[3], (char)bytes[2], (char)bytes[1], (char)bytes[0] });
        }
    }
#endif
}
