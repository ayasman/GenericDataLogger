using System;
using System.Collections.Generic;
using System.Text;

namespace AYLib.GenericDataLogger
{
    /// <summary>
    /// Object to hold data about read data blocks. Primarily for the callback on read, to let
    /// the observing application process incoming data.
    /// </summary>
    public class ReadSerializeData
    {
        /// <summary>
        /// The time that the block was written on (if applicable).
        /// </summary>
        public long Timestamp { get; private set; }

        /// <summary>
        /// The type of data block being read.
        /// </summary>
        public BlockDataTypes BlockType { get; private set; }

        /// <summary>
        /// The actual data object that was deserialized from the read data.
        /// </summary>
        public object DataBlock { get; private set; }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="timeStamp">The time that the block was written on (if applicable)</param>
        /// <param name="dataBlock">The type of data block being read</param>
        /// <param name="blockType">The actual data object that was deserialized from the read data</param>
        public ReadSerializeData(long timeStamp, object dataBlock, BlockDataTypes blockType)
        {
            Timestamp = timeStamp;
            DataBlock = dataBlock;
            BlockType = blockType;
        }
    }
}
