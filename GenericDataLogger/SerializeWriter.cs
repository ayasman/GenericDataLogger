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
    public class SerializeWriter : IDisposable
    {
        private static readonly MessagePackSerializerOptions lz4Options = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);
        private static readonly MessagePackSerializerOptions lz4ContractlessOptions = ContractlessStandardResolver.Options.WithCompression(MessagePackCompression.Lz4BlockArray);

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

        public Header HeaderData => headerData;

        public SerializeWriter(Stream outputStream, bool encode, bool clearBufferOnWrite)
        {
            this.encode = encode;
            this.clearBufferOnWrite = clearBufferOnWrite;
            this.outputStream = outputStream;
            headerWritten = false;
        }

        public SerializeWriter(string fileName, bool encode, bool clearBufferOnWrite)
        {
            this.encode = encode;
            this.clearBufferOnWrite = clearBufferOnWrite;
            headerWritten = false;
            Initialize(fileName);
        }

        public void Initialize(string fileName)
        {
            try
            {
                outputFileName = fileName;
                outputStream = new FileStream(outputFileName, FileMode.Create);
            }
            catch (Exception ex)
            {
                throw new Exception("Error initializing binary file stream.", ex);
            }
        }

        public void RegisterVersion(uint majorVersion, uint minorVersion, uint revision)
        {
            headerData?.RegisterVersion(majorVersion, minorVersion, revision);
        }

        public void RegisterType(Type newType, BlockDataTypes outputType)
        {
            headerData?.RegisterType(newType, outputType);
        }

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
                    throw new Exception("Error updating data cache.", ex);
                }
            }
        }

        public void Write(long timeStamp, ISerializeData data)
        {
            lock (writerLock)
            {
                try
                {
                    if (dataBuffer == null)
                        throw new Exception("Write buffer not open.");

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
                    throw new Exception("Error writing to data buffer.", ex);
                }
            }
        }

        public void WriteBuffer(long timeStamp, bool partial = false)
        {
            lock (writerLock)
            {
                try
                {
                    if (dataBuffer == null)
                        throw new Exception("Write buffer not open.");

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
                    throw new Exception("Error writing to data buffer.", ex);
                }
            }
        }

        public void FlushToStream()
        {
            try
            {
                if (dataBuffer == null)
                    throw new Exception("Write buffer not open.");
                if (outputStream == null)
                    throw new Exception("Cannot write to output stream, no output stream configured.");

                dataBuffer.WriteTo(outputStream);
            }
            catch (Exception ex)
            {
                throw new Exception("Error writing to output stream.", ex);
            }
        }

        private void CreateHeader(long timeStamp)
        {
            dataBuffer.WriteDataBlock(Common.Signature.ToByteArray(), -1, (uint)BlockDataTypes.Signature, timeStamp, encode);
            dataBuffer.WriteDataBlock(
                encode ?
                    MessagePackSerializer.Serialize(headerData, lz4Options) :
                    MessagePackSerializer.Serialize(headerData),
                -1,
                (uint)BlockDataTypes.Header,
                timeStamp,
                encode);
        }

        private byte[] Encode(bool typed, Type dataType, ISerializeData data)
        {
            return typed ?
                encode ?
                    MessagePackSerializer.Serialize(dataType, data, lz4Options) :
                    MessagePackSerializer.Serialize(dataType, data, MessagePackSerializerOptions.Standard)
                        :
                encode ?
                    MessagePackSerializer.Serialize(dataType, data, lz4ContractlessOptions) :
                    MessagePackSerializer.Serialize(dataType, data, MessagePack.Resolvers.ContractlessStandardResolver.Options);
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
