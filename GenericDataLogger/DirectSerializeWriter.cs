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
    /// <summary>
    /// Class to handle the writing of data to a binary stream, without the use of a data cache, but with the same header/data format.
    /// 
    /// This is intended to be used for sending data chunks to the same version of reader, and doesn't include header information as part
    /// of the serialization process (since there is no guarantee that the header will be written in all cases).
    /// 
    /// Thus, this is useful for serializing data for network data exchanges, where two clients have the same data types registered in
    /// the same order.
    /// </summary>
    public class DirectSerializeWriter : IDisposable
    {
        private static readonly MessagePackSerializerOptions lz4Options = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);

        private object writerLock = new object();
        private Header headerData = new Header();

        private readonly bool encode = false;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="encode">If the data is being LZ4 encoded</param>
        public DirectSerializeWriter(bool encode)
        {
            this.encode = encode;
        }

        /// <summary>
        /// Registers a new data type with the system.
        /// </summary>
        /// <param name="newType"></param>
        public void RegisterType(Type newType)
        {
            headerData?.RegisterType(newType, (uint)BlockDataTypes.None);
        }

        /// <summary>
        /// Writes the header/data to the stream as binary data.
        /// </summary>
        /// <param name="outputStream">The stream to write to</param>
        /// <param name="data">The data object to write</param>
        /// <param name="timeStamp">The timestamp to write as, if applicable</param>
        public void Write(Stream outputStream, ISerializeData data, long timeStamp = 0)
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
                                        MessagePackSerializer.Serialize(new BlockMetadata(typeID, timeStamp, dataBytes.Length, (uint)BlockDataTypes.None), lz4Options) :
                                        MessagePackSerializer.Serialize(new BlockMetadata(typeID, timeStamp, dataBytes.Length, (uint)BlockDataTypes.None));

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

        /// <summary>
        /// Serialize the data type using MessagePack.
        /// </summary>
        /// <param name="encode">If the data is to be LZ4 encoded</param>
        /// <param name="dataType">The data type of the object</param>
        /// <param name="data">The object to encode</param>
        /// <returns></returns>
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
