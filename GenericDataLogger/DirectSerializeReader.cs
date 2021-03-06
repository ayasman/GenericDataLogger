using Microsoft.Extensions.Logging;
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
        private object readerLock = new object();
        private Header headerData = new Header();

        private readonly ILogger logger = null;

        private readonly bool encoded = false;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="encoded">If the data is being LZ4 encoded</param>
        /// <param name="logger">The logger to output debug/trace messages to</param>
        public DirectSerializeReader(bool encoded, ILogger logger = null)
        {
            this.logger = logger;
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

                    var metaData = SerializeProvider.DefaultProvider.Decode(true, encoded, typeof(BlockMetadata), metaDataBytes) as BlockMetadata;

                    typeID = metaData.TypeID;
                    timeStamp = metaData.TimeStamp;

                    var dataType = headerData.GetRegistrationType(typeID);
                    if (dataType != null)
                    {
                        byte[] dataBlock = binaryReader.ReadBytes(metaData.BlockSize);

                        var deserializedData = SerializeProvider.CurrentProvider.Decode(true, encoded, dataType, dataBlock);

                        if (logger != null && logger.IsEnabled(LogLevel.Debug))
                        {
                            logger?.LogDebug("Reading Data Block, Timestamp: {timeStamp}, Data Type: {dataType}, Write Type: {writeType}, Data: {data}", timeStamp, dataType, dataType, deserializedData.ToString());
                        }

                        return new ReadSerializeData(timeStamp, deserializedData, BlockDataTypes.None);
                    }
                    else
                        throw new SerializerException("Type not registered.");
                }
            }
            catch (Exception ex)
            {
                throw new SerializerException("Error reading buffer information.", ex);
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
