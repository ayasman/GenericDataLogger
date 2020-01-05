using System;
using System.Collections.Generic;
using System.Text;

namespace AYLib.GenericDataLogger
{
    public class StreamException : Exception
    {
        public StreamException(string message) : 
            base(message)
        {
        }

        public StreamException(string message, Exception innerException) : 
            base(message, innerException)
        {
        }
    }
}
