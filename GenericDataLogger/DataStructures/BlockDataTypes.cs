using System;
using System.Collections.Generic;
using System.Text;

namespace AYLib.GenericDataLogger
{
    [Flags]
    public enum BlockDataTypes : byte
    {
        Signature   = 0b00000,
        Header      = 0b00010,
        Full        = 0b00100,
        Partial     = 0b01000,
        Immediate   = 0b10000
    }
}
