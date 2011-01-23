using System;
using System.Runtime.Serialization;

namespace PSFilterLoad.PSApi
{
    [Serializable]
    class FilterLoadException : Exception, ISerializable
    {
        public FilterLoadException() : base("A FilterLoadException has occured")
        {
            
        }
        public FilterLoadException(string message) : base(message)
        {
            
        }
       
        public FilterLoadException(string message, Exception inner) : base (message, inner)
        {
            
        }

        // This constructor is needed for serialization.
        protected FilterLoadException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            
        }

    }
}
