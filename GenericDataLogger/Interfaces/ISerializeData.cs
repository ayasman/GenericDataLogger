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
        Guid SerializeDataID { get; }
    }
}
