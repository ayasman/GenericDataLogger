using MessagePack;
using System;
using System.Collections.Generic;

namespace AYLib.GenericDataLogger
{
    /// <summary>
    /// What does a file need in order to be appended to but remain a binary/compressable format?
    /// 
    /// Signature - Identifies that this is a format made by the logger. If it isn't the same, we can skip it.
    /// Data Blocks - Continuous data block that contain a size/data pairing. The size is a fixed format that tells us the size of the next byte array.
    /// 
    /// Possible data blocks
    /// Header - Gives the identification of the application and file type that the data contains, and the data formats used
    /// MessagePack Data
    /// Protobuf Data
    /// JSON binary data
    /// </summary>
    public class Class1
    {
        private Guid Signature = Guid.Parse("46429DF1-46C8-4C0D-8479-A3BCB6A87643");
        private Dictionary<string, string> typeList = new Dictionary<string, string>();

        public Class1()
        {
            Signature.ToByteArray();

            typeList.Add("ThisTest", typeof(Class1).FullName);

            var data = MessagePackSerializer.Serialize(typeList);
            var back = MessagePackSerializer.Deserialize<Dictionary<string, string>>(data);

            var bin = MessagePackSerializer.Typeless.Serialize(typeList);
            var objModel = MessagePackSerializer.Typeless.Deserialize(bin) as Dictionary<string, string>;
        }
    }

    internal class DataBlock
    {

    }
}
