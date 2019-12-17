using System;
using System.Collections.Generic;
using System.Text;

namespace AYLib.GenericDataLogger
{
    public class ReadSerializeData
    {
        public long Timestamp { get; private set; }

        public BlockDataTypes BlockType { get; private set; }

        public object DataBlock { get; private set; }

        public ReadSerializeData(long timeStamp, object dataBlock, BlockDataTypes blockType)
        {
            Timestamp = timeStamp;
            DataBlock = dataBlock;
            BlockType = blockType;
        }
    }
}
