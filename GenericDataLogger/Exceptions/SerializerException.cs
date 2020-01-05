using System;
using System.Collections.Generic;
using System.Text;

namespace AYLib.GenericDataLogger
{
    public class SerializerException : Exception
    {
        public SerializerException(string message) : 
            base(message)
        {
        }

        public SerializerException(string message, Exception innerException) : 
            base(message, innerException)
        {
        }
    }
}
