using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AYLib.GenericDataLogger
{
    /// <summary>
    /// Helper class that wraps around a memory stream to read binary data.
    /// </summary>
    public class ReadDataBuffer : IDisposable
    {
        static readonly MessagePackSerializerOptions lz4Options = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);

        private bool bufferFilled = false;
        private MemoryStream memoryStream;
        private BinaryReader binaryReader;
        private object readerLock = new object();

        private long lastBlockStartPosition = 0;

        /// <summary>
        /// True if the memory stream buffer has been written to.
        /// </summary>
        public bool BufferFilled => bufferFilled;

        /// <summary>
        /// Constructor. Initializes the internal memory stream and binary reader for it.
        /// </summary>
        public ReadDataBuffer()
        {
            InitStreams();
        }

        /// <summary>
        /// Initializes the memory streams and reader.
        /// </summary>
        private void InitStreams()
        {
            lock (readerLock)
            {
                try
                {
                    if (binaryReader != null)
                    {
                        binaryReader.Dispose();
                    }

                    memoryStream = new MemoryStream();
                    binaryReader = new BinaryReader(memoryStream, System.Text.Encoding.Default, true);
                }
                catch(Exception ex)
                {
                    throw new Exception("Error creating binary reader streams.", ex);
                }
            }
        }

        /// <summary>
        /// True if the reader cannot be read, or is at the end of a read.
        /// </summary>
        public bool IsEndOfStream
        {
            get
            {
                lock (readerLock)
                {
                    if (binaryReader == null)
                        return true;
                    return binaryReader.BaseStream.Length == binaryReader.BaseStream.Position;
                }
            }
        }

        /// <summary>
        /// Reads the next data block in the data stream and outputs the resulting data.
        /// </summary>
        /// <param name="encoded">If the data block is expected to be encoded</param>
        /// <param name="typeID">The registered type ID for the data block</param>
        /// <param name="blockType">The block type for the data block</param>
        /// <param name="timeStamp">Timestemp of the data block</param>
        /// <param name="overrideReader">A stream to read from, if not using the built-in memory stream</param>
        /// <returns></returns>
        public byte[] ReadDataBlock(bool encoded, out int typeID, out uint blockType, out long timeStamp, BinaryReader overrideReader = null)
        {
            byte[] retBlock = null;
            lock (readerLock)
            {
                try
                {
                    BinaryReader localReader = overrideReader != null ? overrideReader : binaryReader;

                    if (localReader == null)
                        throw new Exception("Binary reader not open.");

                    lastBlockStartPosition = localReader.BaseStream.Position;

                    int metaBlockSize = localReader.ReadInt32();
                    byte[] metaDataBytes = localReader.ReadBytes(metaBlockSize);

                    var metaData = encoded ?
                                    MessagePackSerializer.Deserialize<BlockMetadata>(metaDataBytes, lz4Options) :
                                    MessagePackSerializer.Deserialize<BlockMetadata>(metaDataBytes);

                    retBlock = localReader.ReadBytes(metaData.BlockSize);
                    typeID = metaData.TypeID;
                    blockType = metaData.BlockType;
                    timeStamp = metaData.TimeStamp;
                }
                catch(Exception ex)
                {
                    throw new Exception("Error reading data block information.", ex);
                }
            }
            return retBlock;
        }

        /// <summary>
        /// Copies data from the source stream into the backing memory stream for reading.
        /// </summary>
        /// <param name="source"></param>
        public void ReadFrom(Stream source)
        {
            lock (readerLock)
            {
                try
                {
                    if (memoryStream == null)
                        throw new Exception("Reader memory stream not open.");
                    if (source == null)
                        throw new Exception("Source stream not open.");

                    source.Position = 0;
                    source.CopyTo(memoryStream);
                    memoryStream.Position = 0;
                    bufferFilled = true;
                }
                catch (Exception ex)
                {
                    throw new Exception("Error copying data to memory stream.", ex);
                }
            }
        }

        /// <summary>
        /// Rewinds the position of the memory stream back one data block, to be able to read it again.
        /// Useful when processing to a timestamp, and wanting to rewind when found.
        /// </summary>
        public void RewindOneBlock()
        {
            lock (readerLock)
            {
                if (memoryStream == null)
                    throw new Exception("Reader memory stream not open.");

                memoryStream.Position = lastBlockStartPosition;
            }
        }

        /// <summary>
        /// Resets the position of the memory stream to the start.
        /// </summary>
        public void ResetToStart()
        {
            lock (readerLock)
            {
                if (memoryStream == null)
                    throw new Exception("Reader memory stream not open.");

                memoryStream.Position = 0;
            }
        }

        /// <summary>
        /// True if the backing stream is created and can be read from.
        /// </summary>
        public bool IsStreamOpen => memoryStream != null && memoryStream.CanRead;

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    binaryReader?.Dispose();
                    memoryStream?.Dispose();

                    binaryReader = null;
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
