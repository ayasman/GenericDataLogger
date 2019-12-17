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
                if (binaryReader != null)
                {
                    binaryReader.Dispose();
                }

                memoryStream = new MemoryStream();
                binaryReader = new BinaryReader(memoryStream, System.Text.Encoding.Default, true);
            }
        }

        public bool IsEndOfStream
        {
            get
            {
                lock (readerLock)
                {
                    return binaryReader.BaseStream.Length == binaryReader.BaseStream.Position;
                }
            }
        }

        public byte[] ReadDataBlock(bool encoded, out int typeID, out uint blockType, out long timeStamp, BinaryReader overrideReader = null)
        {
            byte[] retBlock = null;
            lock (readerLock)
            {
                BinaryReader localReader = overrideReader != null ? overrideReader : binaryReader;

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
            return retBlock;
        }

        public void ReadFrom(Stream source)
        {
            lock (readerLock)
            {
                source.Position = 0;
                source.CopyTo(memoryStream);
                memoryStream.Position = 0;
                bufferFilled = true;
            }
        }

        public void RewindOneBlock()
        {
            lock (readerLock)
            {
                memoryStream.Position = lastBlockStartPosition;
            }
        }

        public void ResetToStart()
        {
            lock (readerLock)
            {
                memoryStream.Position = 0;
            }
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    binaryReader.Dispose();
                    memoryStream.Dispose();
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
