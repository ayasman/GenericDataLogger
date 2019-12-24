using MessagePack;
using MessagePack.Resolvers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;

namespace AYLib.GenericDataLogger
{
    public class DirectSerializeWriter : IDisposable
    {
        private static readonly MessagePackSerializerOptions lz4Options = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);

        private object writerLock = new object();
        private Header headerData = new Header();

        private readonly bool encode = false;

        public DirectSerializeWriter(bool encode)
        {
            this.encode = encode;
        }

        public void RegisterType(Type newType)
        {
            headerData?.RegisterType(newType, (uint)BlockDataTypes.None);
        }

        public void Write(Stream outputStream, ISerializeData data)
        {
            lock (writerLock)
            {
                try
                {
                    var dataType = data.GetType();
                    var typeID = headerData.GetRegistrationID(dataType);
                    if (typeID == -2)
                        throw new Exception("Type not registered.");

                    var dataBytes = Encode(encode, dataType, data);

                    var metaBlock = encode ?
                                        MessagePackSerializer.Serialize(new BlockMetadata(typeID, 0, dataBytes.Length, (uint)BlockDataTypes.None), lz4Options) :
                                        MessagePackSerializer.Serialize(new BlockMetadata(typeID, 0, dataBytes.Length, (uint)BlockDataTypes.None));

                    using (var binaryWriter = new BinaryWriter(outputStream, System.Text.Encoding.Default, true))
                    {
                        binaryWriter.Write(metaBlock.Length);
                        binaryWriter.Write(metaBlock);
                        binaryWriter.Write(dataBytes);
                    }
                }
                catch (Exception ex)
                {
                    throw new Exception("Error writing to data buffer.", ex);
                }
            }
        }

        private byte[] Encode(bool encode, Type dataType, ISerializeData data)
        {
            return
                encode ?
                    MessagePackSerializer.Serialize(dataType, data, lz4Options) :
                    MessagePackSerializer.Serialize(dataType, data, MessagePackSerializerOptions.Standard);
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {

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
