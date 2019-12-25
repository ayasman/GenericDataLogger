using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;

namespace AYLib.GenericDataLogger
{
    public class DirectSerializeReader : IDisposable
    {
        static readonly MessagePackSerializerOptions lz4Options = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);

        private object readerLock = new object();
        private Header headerData = new Header();

        private readonly bool encoded = false;

        public DirectSerializeReader(bool encoded)
        {
            this.encoded = encoded;
        }

        public void RegisterType(Type newType)
        {
            headerData?.RegisterType(newType, (uint)BlockDataTypes.None);
        }

        public ReadSerializeData Read(Stream inputStream)
        {
            try
            {
                int typeID = -1;
                long timeStamp = 0;

                using (var binaryReader = new BinaryReader(inputStream, System.Text.Encoding.Default, true))
                {
                    int metaBlockSize = binaryReader.ReadInt32();
                    byte[] metaDataBytes = binaryReader.ReadBytes(metaBlockSize);

                    var metaData = encoded ?
                                    MessagePackSerializer.Deserialize<BlockMetadata>(metaDataBytes, lz4Options) :
                                    MessagePackSerializer.Deserialize<BlockMetadata>(metaDataBytes);

                    typeID = metaData.TypeID;
                    timeStamp = metaData.TimeStamp;

                    var dataType = headerData.GetRegistrationType(typeID);
                    if (dataType != null)
                    {
                        byte[] dataBlock = binaryReader.ReadBytes(metaData.BlockSize);

                        var deserializedData = encoded ?
                                                    MessagePackSerializer.Deserialize(dataType, dataBlock, lz4Options) :
                                                    MessagePackSerializer.Deserialize(dataType, dataBlock);

                        return new ReadSerializeData(timeStamp, deserializedData, BlockDataTypes.None);
                    }
                    else
                        throw new Exception("Type not registered.");
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error reading buffer information.", ex);
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
