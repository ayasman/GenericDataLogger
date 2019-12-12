using MessagePack;
using MessagePack.Formatters;
using MessagePack.LZ4;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

namespace AYLib.GenericDataLogger
{
    [Flags]
    public enum BlockDataTypes : byte
    {
        Header      = 0b0000,
        Full        = 0b0010,
        Partial     = 0b0100,
        Immediate   = 0b1000
    }

    public interface IReplayData
    {
        Guid ReplayDataID { get; }
    }

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
    /// </summary>
    public class ReplayWriter : IDisposable
    {
        protected Guid Signature = Guid.Parse("46429DF1-46C8-4C0D-8479-A3BCB6A87643");

        private Dictionary<Guid, IReplayData> updatedData = new Dictionary<Guid, IReplayData>();
        private HashSet<Guid> recentUpdates = new HashSet<Guid>();

        private object writerLock = new object();

        private readonly bool encode = false;
        private readonly bool clearBufferOnWrite = false;

        private string outputFileName;
        private Stream fileStream;

        private WriteDataBuffer dataBuffer = new WriteDataBuffer();
        private Header headerData = new Header();
        private bool headerWritten = false;

        public ReplayWriter()
        {

        }

        public ReplayWriter(string fileName, bool encode, bool clearBufferOnWrite)
        {
            this.encode = encode;
            this.clearBufferOnWrite = clearBufferOnWrite;

            Initialize(fileName);
        }

        public void Initialize(string fileName)
        {
            outputFileName = fileName;
            fileStream = new FileStream(outputFileName, FileMode.Create);
            headerWritten = false;
        }

        public void RegisterType(Type newType, BlockDataTypes outputType)
        {
            headerData.RegisterType(newType, outputType);
        }

        private void CreateHeader(long timeStamp)
        {
            dataBuffer.WriteDataBlock(Signature.ToByteArray(), -1, (uint)BlockDataTypes.Header, timeStamp, true);
            dataBuffer.WriteDataBlock(
                encode ? LZ4MessagePackSerializer.Serialize(headerData) : MessagePackSerializer.Serialize(headerData), 
                -1, 
                (uint)BlockDataTypes.Header, 
                timeStamp,
                encode);
        }

        public void Update(IReplayData data)
        {
            lock (writerLock)
            {
                if (!updatedData.ContainsKey(data.ReplayDataID))
                    updatedData.Add(data.ReplayDataID, data);
                else
                    updatedData[data.ReplayDataID] = data;

                if (!recentUpdates.Contains(data.ReplayDataID))
                    recentUpdates.Add(data.ReplayDataID);
            }
        }

        public void Write(long timeStamp, IReplayData data)
        {
            lock (writerLock)
            {
                if (!headerWritten)
                    CreateHeader(timeStamp);

                try
                {
                    var dataType = data.GetType();
                    var typeID = headerData.GetRegistrationID(dataType);
                    dataBuffer.WriteDataBlock(
                        encode ? LZ4MessagePackSerializer.NonGeneric.Serialize(dataType, data) : MessagePackSerializer.NonGeneric.Serialize(dataType, data),
                        typeID,
                        (uint)BlockDataTypes.Immediate,
                        timeStamp,
                        encode);
                }
                catch (Exception ex)
                {

                }
            }
        }

        public void WriteBuffer(long timeStamp, bool partial = false)
        {
            lock (writerLock)
            {
                if (!headerWritten)
                    CreateHeader(timeStamp);

                if (partial)
                {
                    foreach(var dataID in recentUpdates)
                    {
                        try
                        {
                            var data = updatedData[dataID];
                            var dataType = data.GetType();

                            if ((headerData.GetRegistrationOutput(dataType) & BlockDataTypes.Partial) == BlockDataTypes.Partial)
                            {
                                var typeID = headerData.GetRegistrationID(dataType);
                                dataBuffer.WriteDataBlock(
                                    encode ? LZ4MessagePackSerializer.NonGeneric.Serialize(dataType, data) : MessagePackSerializer.NonGeneric.Serialize(dataType, data), 
                                    typeID, 
                                    (uint)BlockDataTypes.Partial, 
                                    timeStamp,
                                    encode);
                            }
                        }
                        catch (Exception ex)
                        {

                        }
                    }
                }
                else
                {
                    foreach (var data in updatedData.Values)
                    {
                        try
                        {
                            var dataType = data.GetType();
                            if ((headerData.GetRegistrationOutput(dataType) & BlockDataTypes.Full) == BlockDataTypes.Full)
                            {
                                var typeID = headerData.GetRegistrationID(dataType);
                                dataBuffer.WriteDataBlock(
                                    encode ? LZ4MessagePackSerializer.NonGeneric.Serialize(dataType, data) : MessagePackSerializer.NonGeneric.Serialize(dataType, data), 
                                    typeID, 
                                    (uint)BlockDataTypes.Full, 
                                    timeStamp,
                                    encode);
                            }
                        }
                        catch(Exception ex)
                        {

                        }
                    }
                }

                recentUpdates.Clear();

                if (clearBufferOnWrite)
                    updatedData.Clear();
            }
        }

        public void FlushToFile()
        {
            dataBuffer.WriteTo(fileStream);
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    fileStream.Dispose();
                    dataBuffer.Dispose();
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

    public class ReplayReader
    {
        private readonly bool encoded = false;

        private Stream fileStream;
        private string inputFileName;

        private ReadDataBuffer dataBuffer = new ReadDataBuffer();

        public ReplayReader(string fileName, bool encoded)
        {
            this.encoded = encoded;

            Initialize(fileName);
        }

        public void Initialize(string fileName)
        {
            inputFileName = fileName;
            fileStream = new FileStream(inputFileName, FileMode.Open);
        }
    }

    
    //public class DataLogger : IDisposable
    //{
    //    protected Guid Signature = Guid.Parse("46429DF1-46C8-4C0D-8479-A3BCB6A87643");

    //    public virtual Header HeaderData => null;

    //    public DataLogger()
    //    {

    //    }

    //    #region IDisposable Support
    //    private bool disposedValue = false;

    //    protected virtual void Dispose(bool disposing)
    //    {
    //        if (!disposedValue)
    //        {
    //            if (disposing)
    //            {

    //            }

    //            disposedValue = true;
    //        }
    //    }
    //    public void Dispose()
    //    {
    //        Dispose(true);
    //    }
    //    #endregion
    //}

    //public class DataLoggerWriter : DataLogger
    //{
    //    private WriteDataBuffer dataBuffer = new WriteDataBuffer();
    //    private Header headerData = new Header();
    //    private ConcurrentQueue<byte[]> outputBuffer = new ConcurrentQueue<byte[]>();

    //    public override Header HeaderData => headerData;

    //    public DataLoggerWriter() :
    //        base()
    //    {

    //    }

    //    public void RegisterType(Type newType)
    //    {
    //        headerData.RegisterType(newType);
    //    }

    //    public void CreateHeader()
    //    {
    //        dataBuffer.WriteDataBlock(Signature.ToByteArray(), -1, true);
    //        dataBuffer.WriteDataBlock(MessagePackSerializer.Serialize(headerData), -1);
    //    }

    //    public void WriteData(byte[] data)
    //    {
    //        outputBuffer.Enqueue(data);
    //    }

    //    public void FlushBuffer()
    //    {
    //        byte[] data;
    //        while (outputBuffer.TryDequeue(out data))
    //            dataBuffer.WriteDataBlock(data, -1);
    //    }

    //    public void WriteTo(Stream target)
    //    {
    //        dataBuffer.WriteTo(target);
    //    }

    //    #region IDisposable Support
    //    private bool disposedValue = false;

    //    protected override void Dispose(bool disposing)
    //    {
    //        if (!disposedValue)
    //        {
    //            if (disposing)
    //            {
    //                dataBuffer.Dispose();
    //            }

    //            disposedValue = true;
    //        }
    //        base.Dispose();
    //    }
    //    #endregion
    //}

    public class ReadDataBuffer
    {
        private MemoryStream memoryStream;
        private BinaryReader binaryReader;
        private object readerLock = new object();

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

        public byte[] ReadDataBlock(int typeID, uint blockType, long timeStamp, bool encode, bool isSignature = false)
        {
            byte[] retBlock = null;
            lock (readerLock)
            {
                if (isSignature)
                {
                   // binaryReader.ReadBytes();


                    //var metaBlock = encode ?
                    //                    LZ4MessagePackSerializer.Serialize(new BlockMetadata(typeID, timeStamp, data.Length, blockType)) :
                    //                    MessagePackSerializer.Serialize(new BlockMetadata(typeID, timeStamp, data.Length, blockType));
                    //binaryWriter.Write(metaBlock.Length);
                    //binaryWriter.Write(metaBlock);
                }
                else
                {
                    int metaBlockSize = binaryReader.ReadInt32();
                    byte[] metaDataBytes = binaryReader.ReadBytes(metaBlockSize);

                    var metaData = MessagePackSerializer.Deserialize<BlockMetadata>(metaDataBytes);

                    byte[] dataBytes = binaryReader.ReadBytes(metaData.BlockSize);
                }
                //binaryWriter.Write(data);
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
            }
        }
    }

    public class WriteDataBuffer : IDisposable
    {
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

        public void WriteDataBlock(byte[] data, int typeID, uint blockType, long timeStamp, bool encode, bool isSignature = false)
        {
            lock (writerLock)
            {
                if (!isSignature)
                {
                    var metaBlock = encode ? 
                                        LZ4MessagePackSerializer.Serialize(new BlockMetadata(typeID, timeStamp, data.Length, blockType)) :
                                        MessagePackSerializer.Serialize(new BlockMetadata(typeID, timeStamp, data.Length, blockType));
                    binaryWriter.Write(metaBlock.Length);
                    binaryWriter.Write(metaBlock);
                }
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

    [MessagePackObject]
    public class Header
    {
        private Dictionary<Type, int> registrationIDs = new Dictionary<Type, int>();

        [Key(0)]
        public Dictionary<int, TypeRegistration> TypeRegistrations { get; set; } = new Dictionary<int, TypeRegistration>();

        public Header()
        {

        }

        public void RegisterType(Type newType, BlockDataTypes outputType)
        {
            int id = TypeRegistrations.Count;
            TypeRegistrations.Add(id, new TypeRegistration(id, newType, outputType));
            registrationIDs.Add(newType, id);
        }

        public int GetRegistrationID(Type findType)
        {
            return registrationIDs[findType];
        }

        public BlockDataTypes GetRegistrationOutput(Type findType)
        {
            return TypeRegistrations[registrationIDs[findType]].OutputType;
        }

        public Type GetRegistrationType(int findID)
        {
            return TypeRegistrations[findID].ClassType;
        }
    }

    [MessagePackObject]
    public class TypeRegistration
    {
        private Type linkedType;

        [Key(0)]
        public int RefID { get; set; }

        [Key(1)]
        public string LongName { get; set; }

        [IgnoreMember]
        public Type ClassType
        {
            get
            {
                if (linkedType == null)
                    return Type.GetType(LongName);
                return linkedType;
            }
        }

        [IgnoreMember]
        public BlockDataTypes OutputType { get; set; }

        public TypeRegistration()
        {

        }

        public TypeRegistration(int refID, Type newType, BlockDataTypes outputType)
        {
            linkedType = newType;

            RefID = refID;
            LongName = newType.FullName;
            OutputType = outputType;
        }
    }

    [MessagePackObject]
    public class BlockMetadata
    {
        [Key(0)]
        public int TypeID { get; set; }

        [Key(1)]
        public long TimeStamp { get; set; }

        [Key(2)]
        public int BlockSize { get; set; }

        [Key(3)]
        public uint BlockType { get; set; }

        public BlockMetadata()
        {

        }

        public BlockMetadata(int typeID, long timeStamp, int blockSize, uint blockType)
        {
            TypeID = typeID;
            TimeStamp = timeStamp;
            BlockSize = blockSize;
            BlockType = blockType;
        }
    }
}
