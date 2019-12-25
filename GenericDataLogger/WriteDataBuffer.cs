using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AYLib.GenericDataLogger
{
    public class WriteDataBuffer : IDisposable
    {
        static readonly MessagePackSerializerOptions lz4Options = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);

        private MemoryStream memoryStream;
        private BinaryWriter binaryWriter;
        private object writerLock = new object();

        public WriteDataBuffer()
        {
            InitStreams();
        }

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
                    throw new Exception("Error creating binary writing streams.", ex);
                }
            }
        }

        public void WriteDataBlock(byte[] data, int typeID, uint blockType, long timeStamp, bool encode)
        {
            lock (writerLock)
            {
                try
                {
                    if (binaryWriter == null)
                        throw new Exception("Binary writer not open.");
                    
                    var metaBlock = encode ?
                                        MessagePackSerializer.Serialize(new BlockMetadata(typeID, timeStamp, data.Length, blockType), lz4Options) :
                                        MessagePackSerializer.Serialize(new BlockMetadata(typeID, timeStamp, data.Length, blockType));
                    binaryWriter.Write(metaBlock.Length);
                    binaryWriter.Write(metaBlock);
                    binaryWriter.Write(data);
                }
                catch (Exception ex)
                {
                    throw new Exception("Error writing data block.", ex);
                }
            }
        }

        public void WriteTo(Stream target)
        {
            lock (writerLock)
            {
                try
                {
                    if (memoryStream == null)
                        throw new Exception("Writer memory stream not open.");
                    if (target == null)
                        throw new Exception("Target stream not open.");

                    memoryStream.WriteTo(target);
                    InitStreams();
                }
                catch (Exception ex)
                {
                    throw new Exception("Error writing to target stream.", ex);
                }
            }
        }

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
