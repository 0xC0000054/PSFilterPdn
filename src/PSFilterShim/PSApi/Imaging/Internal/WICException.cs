/////////////////////////////////////////////////////////////////////////////////
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

using TerraFX.Interop.Windows;
using System;
using System.Runtime.InteropServices;

namespace PSFilterLoad.PSApi.Imaging.Internal
{
    internal sealed class WICException : COMException
    {
        public WICException(string message, HRESULT hr) : base(message)
        {
            HResult = hr;
        }

        public static void ThrowIfFailed(string message, HRESULT hr)
        {
            if (hr.FAILED)
            {
                switch ((int)hr)
                {
                    case E.E_OUTOFMEMORY:
                        throw new OutOfMemoryException(message);
                    default:
                        throw new WICException(message, hr);
                }
            }
        }
    }
}
