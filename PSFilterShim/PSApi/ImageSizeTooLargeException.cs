using System;
using System.Runtime.Serialization;

namespace PSFilterLoad.PSApi
{
    [Serializable]
    class ImageSizeTooLargeException : Exception, ISerializable 
    {
        public ImageSizeTooLargeException() : base("An ImageSizeTooLargeException has occured")
        {
        }
        public ImageSizeTooLargeException(string message) : base(message)
        {
        }
        protected ImageSizeTooLargeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }


    }
}
