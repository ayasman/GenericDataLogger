using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;

namespace AYLib.GenericDataLogger
{
    /// <summary>
    /// Class to handle the reading of data from a binary stream, without the use of a data cache, but with the same header/data format.
    /// 
    /// This is intended to be used for receiving data chunks from the same version of writer, and doesn't include header information as part
    /// of the deserialization process (since there is no guarantee that the header will be written in all cases).
    /// 
    /// Thus, this is useful for serializing data for network data exchanges, where two clients have the same data types registered in
    /// the same order.
    /// </summary>
    public class DirectSerializeReader : IDisposable
    {
        static readonly MessagePackSerializerOptions lz4Options = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);

        private object readerLock = new object();
        private Header headerData = new Header();

        private readonly bool encoded = false;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="encode">If the data is being LZ4 encoded</param>
        public DirectSerializeReader(bool encoded)
        {
            this.encoded = encoded;
        }

        /// <summary>
        /// Registers a new data type with the system.
        /// </summary>
        public void RegisterType(Type newType)
        {
            headerData?.RegisterType(newType, (uint)BlockDataTypes.None);
        }

        /// <summary>
        /// Read the next data block from a stream.
        /// </summary>
        /// <param name="inputStream">The stream to read from</param>
        /// <returns>Object with the deserialized data and some header information</returns>
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
