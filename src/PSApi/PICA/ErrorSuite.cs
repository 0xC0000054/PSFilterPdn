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

namespace PSFilterLoad.PSApi.PICA
{
    internal sealed class ErrorSuite
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
            if (zstringSuite == null)
            {
                throw new ArgumentNullException(nameof(zstringSuite));
            }

            setErrorFromPString = new ErrorSuiteSetErrorFromPString(SetErrorFromPString);
            setErrorFromCString = new ErrorSuiteSetErrorFromCString(SetErrorFromCString);
            setErrorFromZString = new ErrorSuiteSetErrorFromZString(SetErrorFromZString);
            this.zstringSuite = zstringSuite;
            errorMessage = null;
        }

        public PSErrorSuite1 CreateErrorSuite1()
        {
            PSErrorSuite1 suite = new()
            {
                SetErrorFromPString = new UnmanagedFunctionPointer<ErrorSuiteSetErrorFromPString>(setErrorFromPString),
                SetErrorFromCString = new UnmanagedFunctionPointer<ErrorSuiteSetErrorFromCString>(setErrorFromCString),
                SetErrorFromZString = new UnmanagedFunctionPointer<ErrorSuiteSetErrorFromZString>(setErrorFromZString)
            };

            return suite;
        }

        private unsafe int SetErrorFromPString(IntPtr str)
        {
            if (str != IntPtr.Zero)
            {
                errorMessage = StringUtil.FromPascalString((byte*)str.ToPointer());

                return PSError.kSPNoError;
            }

            return PSError.kSPBadParameterError;
        }

        private int SetErrorFromCString(IntPtr str)
        {
            if (str != IntPtr.Zero)
            {
                errorMessage = StringUtil.FromCString(str);

                return PSError.kSPNoError;
            }

            return PSError.kSPBadParameterError;
        }

        private int SetErrorFromZString(ASZString str)
        {
            if (zstringSuite.ConvertToString(str, out string value))
            {
                errorMessage = value;

                return PSError.kSPNoError;
            }

            return PSError.kSPBadParameterError;
        }
    }
}
