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

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;
using System;

namespace PSFilterLoad.PSApi.Rendering
{
    internal class Direct2DException : Exception
    {
        public Direct2DException(string message, HRESULT hr) : base(message)
        {
            HResult = hr;
        }

        public Direct2DException(string message, Exception inner) : base(message, inner)
        {
        }

        public static void ThrowIfFailed(string message, HRESULT hr)
        {
            if (hr.FAILED)
            {
                switch ((int)hr)
                {
                    case E.E_OUTOFMEMORY:
                        throw new OutOfMemoryException(message);
                    case D2DERR.D2DERR_RECREATE_TARGET:
                        throw new RecreateTargetException();
                    default:
                        throw new Direct2DException(message, hr);
                }
            }
        }
    }
}
