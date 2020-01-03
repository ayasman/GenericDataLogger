using System;
using System.Collections.Generic;
using System.Text;

namespace AYLib.GenericDataLogger
{
    /// <summary>
    /// Marks the data class as serializable by the reader/writer system.
    /// </summary>
    public interface ISerializeData
    {
        /// <summary>
        /// Unique ID of the data block being serialized. Used to maintain data cache.
        /// </summary>
        Guid SerializeDataID { get; }
    }
}
