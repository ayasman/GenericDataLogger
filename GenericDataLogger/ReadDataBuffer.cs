using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AYLib.GenericDataLogger
{
    public class ReadDataBuffer : IDisposable
    {
        static readonly MessagePackSerializerOptions lz4Options = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);

        private bool bufferFilled = false;
        private MemoryStream memoryStream;
        private BinaryReader binaryReader;
        private object readerLock = new object();

        private long lastBlockStartPosition = 0;

        public bool BufferFilled => bufferFilled;

        public ReadDataBuffer()
        {
            InitStreams();
        }

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

        public void RewindOneBlock()
        {
            lock (readerLock)
            {
                if (memoryStream == null)
                    throw new Exception("Reader memory stream not open.");

                memoryStream.Position = lastBlockStartPosition;
            }
        }

        public void ResetToStart()
        {
            lock (readerLock)
            {
                if (memoryStream == null)
                    throw new Exception("Reader memory stream not open.");

                memoryStream.Position = 0;
            }
        }

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
