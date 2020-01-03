using System;
using System.Collections.Generic;
using System.Text;

namespace AYLib.GenericDataLogger
{
    /// <summary>
    /// Read/Write common information.
    /// </summary>
    public abstract class Common
    {
        /// <summary>
        /// Identifying GUID for files created by the system.
        /// </summary>
        public static Guid Signature = Guid.Parse("46429DF1-46C8-4C0D-8479-A3BCB6A87643");
    }
}
