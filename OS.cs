/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2019 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;

namespace PSFilterPdn
{
    internal static class OS
    {
        private static bool checkedIsVistaOrLater;
        private static bool checkedIsWindows7OrLater;
        private static bool checkedIsWindows8OrLater;
        private static bool isVistaOrLater;
        private static bool isWindows7OrLater;
        private static bool isWindows8OrLater;

        /// <summary>
        /// Gets a value indicating whether the current operating system is Windows Vista or later.
        /// </summary>
        /// <value>
        ///   <c>true</c> if operating system is Windows Vista or later; otherwise, <c>false</c>.
        /// </value>
        public static bool IsVistaOrLater
        {
            get
            {
                if (!checkedIsVistaOrLater)
                {
                    OperatingSystem os = Environment.OSVersion;

                    isVistaOrLater = os.Platform == PlatformID.Win32NT && os.Version.Major >= 6;
                    checkedIsVistaOrLater = true;
                }

                return isVistaOrLater;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the current operating system is Windows 7 or later.
        /// </summary>
        /// <value>
        ///   <c>true</c> if operating system is Windows 7 or later; otherwise, <c>false</c>.
        /// </value>
        public static bool IsWindows7OrLater
        {
            get
            {
                if (!checkedIsWindows7OrLater)
                {
                    OperatingSystem os = Environment.OSVersion;

                    isWindows7OrLater = os.Platform == PlatformID.Win32NT && ((os.Version.Major == 6 && os.Version.Minor >= 1) || os.Version.Major > 6);
                    checkedIsWindows7OrLater = true;
                }

                return isWindows7OrLater;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the current operating system is Windows 8 or later.
        /// </summary>
        /// <value>
        ///   <c>true</c> if operating system is Windows 8 or later; otherwise, <c>false</c>.
        /// </value>
        public static bool IsWindows8OrLater
        {
            get
            {
                if (!checkedIsWindows8OrLater)
                {
                    OperatingSystem os = Environment.OSVersion;

                    isWindows8OrLater = os.Platform == PlatformID.Win32NT && ((os.Version.Major == 6 && os.Version.Minor >= 2) || os.Version.Major > 6);
                    checkedIsWindows8OrLater = true;
                }

                return isWindows8OrLater;
            }
        }
    }
}
