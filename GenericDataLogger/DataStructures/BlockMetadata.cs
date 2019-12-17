using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

namespace AYLib.GenericDataLogger
{
    [MessagePackObject]
    public class BlockMetadata
    {
        [Key(0)]
        public int TypeID { get; set; }

        [Key(1)]
        public long TimeStamp { get; set; }

        [Key(2)]
        public int BlockSize { get; set; }

        [Key(3)]
        public uint BlockType { get; set; }

        public BlockMetadata()
        {

        }

        public BlockMetadata(int typeID, long timeStamp, int blockSize, uint blockType)
        {
            TypeID = typeID;
            TimeStamp = timeStamp;
            BlockSize = blockSize;
            BlockType = blockType;
        }
    }
}
