using System;
using System.Runtime.Serialization;

namespace PSFilterLoad.PSApi
{
    /// <summary>
    /// The exception thrown when loading the PiPL or PiMI resources fails.
    /// </summary>
    [Serializable]
    sealed class FilterLoadException : Exception, ISerializable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FilterLoadException"/> class.
        /// </summary>
        public FilterLoadException() : base()
        {
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="FilterLoadException"/> class.
        /// </summary>
        /// <param name="message">The message to display.</param>
        public FilterLoadException(string message) : base(message)
        {
        }
       
        // This constructor is needed for serialization.
        private FilterLoadException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            
        }

    }
}
