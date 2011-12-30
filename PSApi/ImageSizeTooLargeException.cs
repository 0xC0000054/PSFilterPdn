using System;
using System.Runtime.Serialization;

namespace PSFilterLoad.PSApi
{
    /// <summary>
    /// The exception that occurs when the image size exceeds 32000 pixels.
    /// </summary>
    [Serializable]
    public sealed class ImageSizeTooLargeException : Exception, ISerializable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ImageSizeTooLargeException"/> class.
        /// </summary>
        public ImageSizeTooLargeException()
            : base("An ImageSizeTooLargeException has occured")
        {
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="ImageSizeTooLargeException"/> class.
        /// </summary>
        /// <param name="message">The message to display.</param>
        public ImageSizeTooLargeException(string message)
            : base(message)
        {
        }
        private ImageSizeTooLargeException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }


    }
}
