/////////////////////////////////////////////////////////////////////////////////
//
// Photoshop-compatible filter host Effect plugin for Paint.NET
// http://psfilterpdn.codeplex.com/
//
// This software is provided under the Microsoft Public License:
//   Copyright (C) 2010-2015 Nicholas Hayes
// 
// See LICENSE.txt for complete licensing and attribution information.
//
/////////////////////////////////////////////////////////////////////////////////

using System;
using System.Runtime.Serialization;

namespace PSFilterLoad.PSApi
{
    /// <summary>
    /// The exception that occurs when the image size exceeds 32000 pixels in width or height.
    /// </summary>
    [Serializable]
    public sealed class ImageSizeTooLargeException : Exception, ISerializable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImageSizeTooLargeException"/> class.
        /// </summary>
        public ImageSizeTooLargeException() : base("An ImageSizeTooLargeException has occurred")
        {
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="ImageSizeTooLargeException"/> class.
        /// </summary>
        /// <param name="message">The message to display.</param>
        public ImageSizeTooLargeException(string message) : base(message)
        {
        }
        private ImageSizeTooLargeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }


    }
}
