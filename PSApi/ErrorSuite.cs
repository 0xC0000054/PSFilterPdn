/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// http://psfilterpdn.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2016 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi
{
    internal sealed class ErrorSuite
    {
        private readonly ErrorSuiteSetErrorFromPString setErrorFromPString;
        private readonly ErrorSuiteSetErrorFromCString setErrorFromCString;
        private readonly ErrorSuiteSetErrorFromZString setErrorFromZString;
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

        public ErrorSuite()
        {
            this.setErrorFromPString = new ErrorSuiteSetErrorFromPString(SetErrorFromPString);
            this.setErrorFromCString = new ErrorSuiteSetErrorFromCString(SetErrorFromCString);
            this.setErrorFromZString = new ErrorSuiteSetErrorFromZString(SetErrorFromZString);
            this.errorMessage = null;
        }

        public PSErrorSuite1 CreateErrorSuite1()
        {
            PSErrorSuite1 suite = new PSErrorSuite1();
            suite.SetErrorFromPString = Marshal.GetFunctionPointerForDelegate(this.setErrorFromPString);
            suite.SetErrorFromCString = Marshal.GetFunctionPointerForDelegate(this.setErrorFromCString);
            suite.SetErrorFromZString = Marshal.GetFunctionPointerForDelegate(this.setErrorFromZString);

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
            if (PICA.ASZStringSuite.Instance.ConvertToString(str, out value))
            {
                this.errorMessage = value;

                return PSError.kSPNoError;
            }

            return PSError.kSPBadParameterError;
        }
    }
}
