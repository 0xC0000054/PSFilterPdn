/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// https://github.com/0xC0000054/PSFilterPdn
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2017 Nicholas Hayes
//
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using PSFilterLoad.PSApi.PICA;
using System;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    internal sealed class ErrorSuite
    {
        private readonly ErrorSuiteSetErrorFromPString setErrorFromPString;
        private readonly ErrorSuiteSetErrorFromCString setErrorFromCString;
        private readonly ErrorSuiteSetErrorFromZString setErrorFromZString;
        private readonly IASZStringSuite zstringSuite;
        private string errorMessage;

        public string ErrorMessage
        {
            get
            {
                return this.errorMessage;
            }
        }

        public bool HasErrorMessage
        {
            get
            {
                return this.errorMessage != null;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ErrorSuite"/> class.
        /// </summary>
        /// <param name="zstringSuite">The ASZString suite instance.</param>
        /// <exception cref="ArgumentNullException"><paramref name="zstringSuite"/> is null.</exception>
        public ErrorSuite(IASZStringSuite zstringSuite)
        {
            if (zstringSuite == null)
            {
                throw new ArgumentNullException("zstringSuite");
            }

            this.setErrorFromPString = new ErrorSuiteSetErrorFromPString(SetErrorFromPString);
            this.setErrorFromCString = new ErrorSuiteSetErrorFromCString(SetErrorFromCString);
            this.setErrorFromZString = new ErrorSuiteSetErrorFromZString(SetErrorFromZString);
            this.zstringSuite = zstringSuite;
            this.errorMessage = null;
        }

        public PSErrorSuite1 CreateErrorSuite1()
        {
            PSErrorSuite1 suite = new PSErrorSuite1
            {
                SetErrorFromPString = Marshal.GetFunctionPointerForDelegate(this.setErrorFromPString),
                SetErrorFromCString = Marshal.GetFunctionPointerForDelegate(this.setErrorFromCString),
                SetErrorFromZString = Marshal.GetFunctionPointerForDelegate(this.setErrorFromZString)
            };

            return suite;
        }

        private unsafe int SetErrorFromPString(IntPtr str)
        {
            if (str != IntPtr.Zero)
            {
                this.errorMessage = StringUtil.FromPascalString((byte*)str.ToPointer());

                return PSError.kSPNoError;
            }

            return PSError.kSPBadParameterError;
        }

        private int SetErrorFromCString(IntPtr str)
        {
            if (str != IntPtr.Zero)
            {
                this.errorMessage = Marshal.PtrToStringAnsi(str);

                return PSError.kSPNoError;
            }

            return PSError.kSPBadParameterError;
        }

        private int SetErrorFromZString(IntPtr str)
        {
            string value;
            if (zstringSuite.ConvertToString(str, out value))
            {
                this.errorMessage = value;

                return PSError.kSPNoError;
            }

            return PSError.kSPBadParameterError;
        }
    }
}
