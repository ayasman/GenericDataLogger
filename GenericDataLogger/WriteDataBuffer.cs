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
                if (binaryWriter != null)
                {
                    binaryWriter.Dispose();
                }

                memoryStream = new MemoryStream();
                binaryWriter = new BinaryWriter(memoryStream, System.Text.Encoding.Default, true);
            }
        }

        public void WriteDataBlock(byte[] data, int typeID, uint blockType, long timeStamp, bool encode)
        {
            lock (writerLock)
            {
                //if (!isSignature)
                //{
                var metaBlock = encode ?
                                    MessagePackSerializer.Serialize(new BlockMetadata(typeID, timeStamp, data.Length, blockType), lz4Options) :
                                    MessagePackSerializer.Serialize(new BlockMetadata(typeID, timeStamp, data.Length, blockType));
                binaryWriter.Write(metaBlock.Length);
                binaryWriter.Write(metaBlock);
                //}
                //else
                //{
                //    binaryWriter.Write(data.Length);
                //}
                binaryWriter.Write(data);
            }
        }

        public void WriteTo(Stream target)
        {
            lock (writerLock)
            {
                memoryStream.WriteTo(target);
                InitStreams();
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
                    // TODO: dispose managed state (managed objects).
                    binaryWriter.Dispose();
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
