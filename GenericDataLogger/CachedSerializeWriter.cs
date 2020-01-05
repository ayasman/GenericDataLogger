using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;

namespace AYLib.GenericDataLogger
{
    /// <summary>
    /// Class to handle the writing of data to a binary stream.
    /// 
    /// Uses a data cache. The calling application updates the data in the cache then, when needed, flushes this data to a memory stream using
    /// the WriteBuffer method. 
    /// 
    /// This data remains in memory until the calling application uses the FlushToStream method, which pushes the data to the output stream and
    /// resets the internal cache stream.
    /// 
    /// The intent is to allow the application to control at what points data is written to the buffer, as well as control when the data is serialized
    /// to the disk or other media (as this could be a fairly intensive operation).
    /// 
    /// ------------------------
    /// 
    /// What does a file need in order to be appended to but remain a binary/compressable format?
    /// 
    /// Signature - Identifies that this is a format made by the logger. If it isn't the same, we can skip it.
    /// Data Blocks - Continuous data block that contain a size/data pairing. The size is a fixed format that tells us the size of the next byte array.
    /// 
    /// Possible data blocks
    /// Header - Gives the identification of the application and file type that the data contains, and the data formats used
    /// MessagePack Data
    /// Protobuf Data
    /// JSON binary data
    /// 
    /// https://github.com/neuecc/MessagePack-CSharp/blob/1ff44c22065df8dac6fe73aa88510570611c142e/doc/migration.md
    /// 
    /// </summary>
    public class CachedSerializeWriter : IDisposable
    {
        private Dictionary<Guid, ISerializeData> updatedData = new Dictionary<Guid, ISerializeData>();
        private HashSet<Guid> recentUpdates = new HashSet<Guid>();

        private object writerLock = new object();

        private readonly bool encode = false;
        private readonly bool clearBufferOnWrite = false;

        private string outputFileName;
        private Stream outputStream;

        private WriteDataBuffer dataBuffer = new WriteDataBuffer();
        private Header headerData = new Header();
        private bool headerWritten = false;

        /// <summary>
        /// Current header data for the cached data.
        /// </summary>
        public Header HeaderData => headerData;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="outputStream">The stream that will be written to on a call to FlushToStream</param>
        /// <param name="encode">If the data should be LZ4 encoded</param>
        /// <param name="clearBufferOnWrite">If the data cache should be cleared when the data is pushed to the buffer</param>
        public CachedSerializeWriter(Stream outputStream, bool encode, bool clearBufferOnWrite)
        {
            this.encode = encode;
            this.clearBufferOnWrite = clearBufferOnWrite;
            this.outputStream = outputStream;
            headerWritten = false;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="outputStream">The name/location of the file to write to on call to FlushToStream</param>
        /// <param name="encode">If the data should be LZ4 encoded</param>
        /// <param name="clearBufferOnWrite">If the data cache should be cleared when the data is pushed to the buffer</param>
        public CachedSerializeWriter(string fileName, bool encode, bool clearBufferOnWrite)
        {
            this.encode = encode;
            this.clearBufferOnWrite = clearBufferOnWrite;
            headerWritten = false;
            Initialize(fileName);
        }

        /// <summary>
        /// Initializes an output file for the FlushToStream call.
        /// </summary>
        /// <param name="fileName">The name/location of the file to write to</param>
        public void Initialize(string fileName)
        {
            try
            {
                outputFileName = fileName;
                outputStream = new FileStream(outputFileName, FileMode.Create);
            }
            catch (Exception ex)
            {
                throw new StreamException("Error initializing binary file stream.", ex);
            }
        }

        /// <summary>
        /// Registers new version information to the header.
        /// </summary>
        /// <param name="majorVersion">Major version number</param>
        /// <param name="minorVersion">Minor version number</param>
        /// <param name="revision">Revision number</param>
        public void RegisterVersion(uint majorVersion, uint minorVersion, uint revision)
        {
            headerData?.RegisterVersion(majorVersion, minorVersion, revision);
        }

        /// <summary>
        /// Registers a new data type with the system.
        /// </summary>
        /// <param name="newType">The type to register</param>
        /// <param name="outputType">The output type this is valid for (partial or full writes)</param>
        public void RegisterType(Type newType, BlockDataTypes outputType)
        {
            headerData?.RegisterType(newType, outputType);
        }

        /// <summary>
        /// Pushes a data object to the cache. If the serialization ID exists, the object is replaced.
        /// </summary>
        /// <param name="data">Updated data object</param>
        public void Update(ISerializeData data)
        {
            lock (writerLock)
            {
                try
                {
                    if (!updatedData.ContainsKey(data.SerializeDataID))
                        updatedData.Add(data.SerializeDataID, data);
                    else
                        updatedData[data.SerializeDataID] = data;

                    if (!recentUpdates.Contains(data.SerializeDataID))
                        recentUpdates.Add(data.SerializeDataID);
                }
                catch(Exception ex)
                {
                    throw new SerializerException("Error updating data cache.", ex);
                }
            }
        }

        /// <summary>
        /// Writes data directly to the data buffer, skipping the cache.
        /// </summary>
        /// <param name="timeStamp">Timestamp of the data write</param>
        /// <param name="data">Data object to serialize</param>
        public void Write(long timeStamp, ISerializeData data)
        {
            lock (writerLock)
            {
                try
                {
                    if (dataBuffer == null)
                        throw new StreamException("Write buffer not open.");

                    if (!headerWritten)
                    {
                        CreateHeader(timeStamp);
                        headerWritten = true;
                    }

                    var dataType = data.GetType();
                    var typeID = headerData.GetRegistrationID(dataType);
                    dataBuffer.WriteDataBlock(
                        Encode(typeID >= 0, dataType, data),
                        typeID,
                        (uint)BlockDataTypes.Immediate,
                        timeStamp,
                        encode);
                }
                catch (Exception ex)
                {
                    throw new SerializerException("Error writing to data buffer.", ex);
                }
            }
        }

        /// <summary>
        /// Writes data from the cache to the data buffer, then clears the cache if requested.
        /// 
        /// Has two primary write modes, a full and partial. A full write pushes all data from the cache
        /// (with data types marked with Full or None), while a partial write only pushes cache data that has been
        /// updated since the last write (and marked with Partial or None).
        /// 
        /// If a header hasn't been output before writing data, it will do that first.
        /// </summary>
        /// <param name="timeStamp"></param>
        /// <param name="partial"></param>
        public void WriteBuffer(long timeStamp, bool partial = false)
        {
            lock (writerLock)
            {
                try
                {
                    if (dataBuffer == null)
                        throw new StreamException("Write buffer not open.");

                    if (!headerWritten)
                    {
                        CreateHeader(timeStamp);
                        headerWritten = true;
                    }

                    if (partial)
                    {
                        foreach (var dataID in recentUpdates)
                        {
                            if (updatedData.ContainsKey(dataID))
                            {
                                var data = updatedData[dataID];
                                var dataType = data.GetType();
                                var outputType = headerData.GetRegistrationOutput(dataType);

                                if ((outputType & BlockDataTypes.Partial) == BlockDataTypes.Partial ||
                                     outputType == BlockDataTypes.None)
                                {
                                    var typeID = headerData.GetRegistrationID(dataType);
                                    dataBuffer.WriteDataBlock(
                                        Encode(typeID >= 0, dataType, data),
                                        typeID,
                                        (uint)BlockDataTypes.Partial,
                                        timeStamp,
                                        encode);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (updatedData != null)
                        {
                            foreach (var data in updatedData.Values)
                            {
                                var dataType = data.GetType();
                                var outputType = headerData.GetRegistrationOutput(dataType);

                                if ((outputType & BlockDataTypes.Full) == BlockDataTypes.Full ||
                                     outputType == BlockDataTypes.None)
                                {
                                    var typeID = headerData.GetRegistrationID(dataType);
                                    dataBuffer.WriteDataBlock(
                                        Encode(typeID >= 0, dataType, data),
                                        typeID,
                                        (uint)BlockDataTypes.Full,
                                        timeStamp,
                                        encode);
                                }
                            }
                        }
                    }

                    recentUpdates.Clear();

                    if (clearBufferOnWrite)
                        updatedData.Clear();
                }
                catch (Exception ex)
                {
                    throw new SerializerException("Error writing to data buffer.", ex);
                }
            }
        }

        /// <summary>
        /// Writes the data in the buffer to the output stream (file or otherwise).
        /// </summary>
        public void FlushToStream()
        {
            try
            {
                if (dataBuffer == null)
                    throw new StreamException("Write buffer not open.");
                if (outputStream == null)
                    throw new StreamException("Cannot write to output stream, no output stream configured.");

                dataBuffer.WriteTo(outputStream);
            }
            catch (Exception ex)
            {
                throw new SerializerException("Error writing to output stream.", ex);
            }
        }

        /// <summary>
        /// Writes the header data block to the buffer.
        /// </summary>
        /// <param name="timeStamp"></param>
        private void CreateHeader(long timeStamp)
        {
            dataBuffer.WriteDataBlock(Common.Signature.ToByteArray(), -1, (uint)BlockDataTypes.Signature, timeStamp, false);
            dataBuffer.WriteDataBlock(BitConverter.GetBytes(encode), -1, (uint)BlockDataTypes.Signature, timeStamp, false);
            dataBuffer.WriteDataBlock(
                SerializeProvider.DefaultProvider.Encode(true, encode, typeof(Header), headerData),
                -1,
                (uint)BlockDataTypes.Header,
                timeStamp,
                encode);
        }

        /// <summary>
        /// Encodes a data object to a byte array for use in the binary write.
        /// </summary>
        /// <param name="typed">If the data has a registered type</param>
        /// <param name="dataType">The data type to serialize as</param>
        /// <param name="data">Data object to serialize</param>
        /// <returns>Serialized data</returns>
        private byte[] Encode(bool typed, Type dataType, ISerializeData data)
        {
            return SerializeProvider.CurrentProvider.Encode(typed, encode, dataType, data);
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    outputStream?.Dispose();
                    dataBuffer?.Dispose();

                    outputStream = null;
                    dataBuffer = null;
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
