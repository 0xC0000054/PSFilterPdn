﻿/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the MIT License:
//   Copyright (C) 2010-2024 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;

namespace PSFilterLoad.PSApi.PICA
{
    internal sealed class ErrorSuite : IPICASuiteAllocator
    {
        private readonly ErrorSuiteSetErrorFromPString setErrorFromPString;
        private readonly ErrorSuiteSetErrorFromCString setErrorFromCString;
        private readonly ErrorSuiteSetErrorFromZString setErrorFromZString;
        private readonly IASZStringSuite zstringSuite;
        private string errorMessage;

        public string ErrorMessage => errorMessage;

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorSuite"/> class.
        /// </summary>
        /// <param name="zstringSuite">The ASZString suite instance.</param>
        /// <exception cref="ArgumentNullException"><paramref name="zstringSuite"/> is null.</exception>
        public ErrorSuite(IASZStringSuite zstringSuite)
        {
            ArgumentNullException.ThrowIfNull(zstringSuite, nameof(zstringSuite));

            setErrorFromPString = new ErrorSuiteSetErrorFromPString(SetErrorFromPString);
            setErrorFromCString = new ErrorSuiteSetErrorFromCString(SetErrorFromCString);
            setErrorFromZString = new ErrorSuiteSetErrorFromZString(SetErrorFromZString);
            this.zstringSuite = zstringSuite;
            errorMessage = string.Empty;
        }

        unsafe IntPtr IPICASuiteAllocator.Allocate(int version)
        {
            if (!IsSupportedVersion(version))
            {
                throw new UnsupportedPICASuiteVersionException(PSConstants.PICA.ErrorSuite, version);
            }

            PSErrorSuite1* suite = Memory.Allocate<PSErrorSuite1>(MemoryAllocationOptions.Default);

            suite->SetErrorFromPString = new UnmanagedFunctionPointer<ErrorSuiteSetErrorFromPString>(setErrorFromPString);
            suite->SetErrorFromCString = new UnmanagedFunctionPointer<ErrorSuiteSetErrorFromCString>(setErrorFromCString);
            suite->SetErrorFromZString = new UnmanagedFunctionPointer<ErrorSuiteSetErrorFromZString>(setErrorFromZString);

            return new IntPtr(suite);
        }

        bool IPICASuiteAllocator.IsSupportedVersion(int version) => IsSupportedVersion(version);

        public static bool IsSupportedVersion(int version) => version == 1;

        private unsafe int SetErrorFromPString(IntPtr str)
        {
            if (str != IntPtr.Zero)
            {
                errorMessage = StringUtil.FromPascalString((byte*)str)!;

                return PSError.kSPNoError;
            }

            return PSError.kSPBadParameterError;
        }

        private int SetErrorFromCString(IntPtr str)
        {
            if (str != IntPtr.Zero)
            {
                errorMessage = StringUtil.FromCString(str)!;

                return PSError.kSPNoError;
            }

            return PSError.kSPBadParameterError;
        }

        private int SetErrorFromZString(ASZString str)
        {
            if (zstringSuite.ConvertToString(str, out string? value))
            {
                errorMessage = value;

                return PSError.kSPNoError;
            }

            return PSError.kSPBadParameterError;
        }
    }
}
