using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AYLib.GenericDataLogger
{
    /// <summary>
    /// Helper class that wraps around a memory stream to write binary data in a common header/block format, using
    /// MessagePack as the main read/write library.
    /// </summary>
    public class WriteDataBuffer : IDisposable
    {
        private MemoryStream memoryStream;
        private BinaryWriter binaryWriter;
        private object writerLock = new object();

        /// <summary>
        /// Constructor. Initializes the internal memory stream and binary writer for it.
        /// </summary>
        public WriteDataBuffer()
        {
            InitStreams();
        }

        /// <summary>
        /// Initializes the memory streams and writer.
        /// </summary>
        private void InitStreams()
        {
            lock (writerLock)
            {
                try
                {
                    if (binaryWriter != null)
                    {
                        binaryWriter.Dispose();
                    }

                    memoryStream = new MemoryStream();
                    binaryWriter = new BinaryWriter(memoryStream, System.Text.Encoding.Default, true);
                }
                catch (Exception ex)
                {
                    throw new StreamException("Error creating binary writing streams.", ex);
                }
            }
        }

        /// <summary>
        /// Writes a block of data to the backing stream. First the length of a header block, then the header block, then the data block.
        /// </summary>
        /// <param name="data">The actual data block to write to the stream (already encoded)</param>
        /// <param name="typeID">Registration ID for the data type</param>
        /// <param name="blockType">The block type for the written data</param>
        /// <param name="timeStamp">Write time of the data</param>
        /// <param name="encode">If the data should be LZ4 encoded</param>
        public void WriteDataBlock(byte[] data, int typeID, uint blockType, long timeStamp, bool encode)
        {
            lock (writerLock)
            {
                try
                {
                    if (binaryWriter == null)
                        throw new StreamException("Binary writer not open.");

                    var metaBlock = SerializeProvider.DefaultProvider.Encode(true, encode, typeof(BlockMetadata), new BlockMetadata(typeID, timeStamp, data.Length, blockType));

                    binaryWriter.Write(metaBlock.Length);
                    binaryWriter.Write(metaBlock);
                    binaryWriter.Write(data);
                }
                catch (Exception ex)
                {
                    throw new SerializerException("Error writing data block.", ex);
                }
            }
        }

        /// <summary>
        /// Writes the backing stream data to a target stream, then resets the backing stream (re-initialize).
        /// </summary>
        /// <param name="target">Target stream to write to</param>
        public void WriteTo(Stream target)
        {
            lock (writerLock)
            {
                try
                {
                    if (memoryStream == null)
                        throw new StreamException("Writer memory stream not open.");
                    if (target == null)
                        throw new StreamException("Target stream not open.");

                    memoryStream.WriteTo(target);
                    InitStreams();
                }
                catch (Exception ex)
                {
                    throw new SerializerException("Error writing to target stream.", ex);
                }
            }
        }

        /// <summary>
        /// True if the backing stream is created and can be written to.
        /// </summary>
        public bool IsStreamOpen => memoryStream != null && memoryStream.CanWrite;

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    binaryWriter?.Dispose();
                    memoryStream?.Dispose();

                    binaryWriter = null;
                    memoryStream = null;
                }

                disposedValue = true;
            }
        }
        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
