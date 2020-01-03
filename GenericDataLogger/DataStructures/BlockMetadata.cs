using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

namespace AYLib.GenericDataLogger
{
    /// <summary>
    /// The definition of the meta information block that identifies the written data and its type.
    /// The block size identifies the size of the data to read.
    /// </summary>
    [MessagePackObject]
    public class BlockMetadata
    {
        /// <summary>
        /// The registered ID for the data type this header is created for.
        /// </summary>
        [Key(0)]
        public int TypeID { get; set; }

        /// <summary>
        /// The timestamp for the write time of the data.
        /// </summary>
        [Key(1)]
        public long TimeStamp { get; set; }

        /// <summary>
        /// The size of the data block that was written.
        /// </summary>
        [Key(2)]
        public int BlockSize { get; set; }

        /// <summary>
        /// The type of block the write was (full/partial/etc.)
        /// </summary>
        [Key(3)]
        public uint BlockType { get; set; }

        /// <summary>
        /// Default constructor, required for deserialization through MessagePack.
        /// </summary>
        public BlockMetadata()
        {

        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="typeID">The registered ID for the data type this header is created for</param>
        /// <param name="timeStamp">The timestamp for the relative write time of the data.</param>
        /// <param name="blockSize">The size of the data block that was written</param>
        /// <param name="blockType">The type of block the write was (full/partial/etc.)</param>
        public BlockMetadata(int typeID, long timeStamp, int blockSize, uint blockType)
        {
            TypeID = typeID;
            TimeStamp = timeStamp;
            BlockSize = blockSize;
            BlockType = blockType;
        }
    }
}
