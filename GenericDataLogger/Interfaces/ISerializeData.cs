using System;
using System.Collections.Generic;
using System.Text;

namespace AYLib.GenericDataLogger
{
    public interface ISerializeData
    {
        Guid SerializeDataID { get; }
    }
}
