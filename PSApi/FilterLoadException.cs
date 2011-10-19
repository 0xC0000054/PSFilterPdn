using System;
using System.Runtime.Serialization;

namespace PSFilterLoad.PSApi
{
    /// <summary>
    /// The exception thrown when loading the PiPL or PiMI resources fails.
    /// </summary>
    [Serializable]
    class FilterLoadException : Exception, ISerializable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FilterLoadException"/> class.
        /// </summary>
        public FilterLoadException() : base("A FilterLoadException has occured")
        {
            
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="FilterLoadException"/> class.
        /// </summary>
        /// <param name="message">The message to display.</param>
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
