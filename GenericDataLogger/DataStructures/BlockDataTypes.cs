using System;
using System.Collections.Generic;
using System.Text;

namespace AYLib.GenericDataLogger
{
    /// <summary>
    /// Primary types of data blocks available for writing.
    /// External applications should really only use the Full and Partial types.
    /// </summary>
    [Flags]
    public enum BlockDataTypes : byte
    {
        None        = 0b000000,
        Signature   = 0b000010,
        Header      = 0b000100,
        Full        = 0b001000,
        Partial     = 0b010000,
        Immediate   = 0b100000
    }
}
