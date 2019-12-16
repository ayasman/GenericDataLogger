using MessagePack;
using MessagePack.Formatters;
using MessagePack.LZ4;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace AYLib.GenericDataLogger
{
    [Flags]
    public enum BlockDataTypes : byte
    {
        Signature   = 0b00000,
        Header      = 0b00010,
        Full        = 0b00100,
        Partial     = 0b01000,
        Immediate   = 0b10000
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
        public static Guid Signature = Guid.Parse("46429DF1-46C8-4C0D-8479-A3BCB6A87643");

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

        public Header HeaderData => headerData;

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

        public void RegisterVersion(uint majorVersion, uint minorVersion, uint revision)
        {
            headerData.RegisterVersion(majorVersion, minorVersion, revision);
        }

        public void RegisterType(Type newType, BlockDataTypes outputType)
        {
            headerData.RegisterType(newType, outputType);
        }

        private void CreateHeader(long timeStamp)
        {
            dataBuffer.WriteDataBlock(Signature.ToByteArray(), -1, (uint)BlockDataTypes.Signature, timeStamp, encode);
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

    public class ReadReplayData
    {
        public long Timestamp { get; private set; }

        public BlockDataTypes BlockType { get; private set; }

        public object DataBlock { get; private set; }

        public ReadReplayData(long timeStamp, object dataBlock, BlockDataTypes blockType)
        {
            Timestamp = timeStamp;
            DataBlock = dataBlock;
            BlockType = blockType;
        }
    }

    public class ReplayReader : IDisposable
    {
        private Subject<ReadReplayData> onDataRead = new Subject<ReadReplayData>();

        private readonly bool encoded = false;

        private Guid signature;
        private Header headerData;

        private Stream fileStream;
        private string inputFileName;

        private ReadDataBuffer dataBuffer = new ReadDataBuffer();

        public IObservable<ReadReplayData> WhenDataRead => onDataRead.Publish().RefCount();

        public Guid Signature => signature;

        public Header HeaderData => headerData;
        
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

        public void ReadFromFile()
        {
            dataBuffer.ReadFrom(fileStream);
        }

        public void ReadHeader()
        {
            byte[] sigData = null;
            byte[] header = null;
            if (dataBuffer.BufferFilled)
            {
                dataBuffer.ResetToStart();
                sigData = dataBuffer.ReadDataBlock(encoded, out int sigTypeID, out uint sigBlockType, out long sigTimeStamp);
                header = dataBuffer.ReadDataBlock(encoded, out int typeID, out uint blockType, out long timeStamp);
            }
            else
            {
                var fileReader = new BinaryReader(fileStream, System.Text.Encoding.Default, true);
                sigData = dataBuffer.ReadDataBlock(encoded, out int sigTypeID, out uint sigBlockType, out long sigTimeStamp, fileReader);
                header = dataBuffer.ReadDataBlock(encoded, out int typeID, out uint blockType, out long timeStamp, fileReader);
                fileReader.Dispose();
            }
            signature = new Guid(sigData);
            headerData = encoded ?
                LZ4MessagePackSerializer.Deserialize<Header>(header) :
                MessagePackSerializer.Deserialize<Header>(header);
            headerData.ResetRegistrationIDs();
        }

        public void ReadData(long timeToReadTo = long.MaxValue)
        {
            var fileReader = new BinaryReader(fileStream, System.Text.Encoding.Default, true);

            bool doRead = true;
            while (doRead)
            {
                byte[] dataBlock = null;
                int typeID = -1;
                uint blockType;
                long timeStamp;

                if (dataBuffer.BufferFilled)
                {
                    if (dataBuffer.IsEndOfStream)
                        break;
                    dataBlock = dataBuffer.ReadDataBlock(encoded, out typeID, out blockType, out timeStamp);
                }
                else
                {
                    if (fileStream.Length == fileStream.Position)
                        break;
                    dataBlock = dataBuffer.ReadDataBlock(encoded, out typeID, out blockType, out timeStamp, fileReader);
                }

                if (timeToReadTo!= long.MaxValue && timeToReadTo >= timeStamp)
                {
                    dataBuffer.RewindOneBlock();
                    break;
                }

                var dataType = headerData.GetRegistrationType(typeID);
                if (dataType != null)
                {
                    var deserializedData = encoded ?
                                                LZ4MessagePackSerializer.NonGeneric.Deserialize(dataType, dataBlock) :
                                                MessagePackSerializer.NonGeneric.Deserialize(dataType, dataBlock);
                    onDataRead.OnNext(new ReadReplayData(timeStamp, deserializedData, (BlockDataTypes)blockType));
                }
            }

            fileReader.Dispose();
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
                    onDataRead.Dispose();
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

    public class ReadDataBuffer : IDisposable
    {
        private bool bufferFilled = false;
        private MemoryStream memoryStream;
        private BinaryReader binaryReader;
        private object readerLock = new object();

        private long lastBlockStartPosition = 0;

        public bool BufferFilled => bufferFilled;

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

        public bool IsEndOfStream
        {
            get
            {
                lock (readerLock)
                {
                    return binaryReader.BaseStream.Length == binaryReader.BaseStream.Position;
                }
            }
        }

        public byte[] ReadDataBlock(bool encoded, out int typeID, out uint blockType, out long timeStamp, BinaryReader overrideReader = null)
        {
            byte[] retBlock = null;
            lock (readerLock)
            {
                BinaryReader localReader = overrideReader != null ? overrideReader : binaryReader;

                lastBlockStartPosition = localReader.BaseStream.Position;

                int metaBlockSize = localReader.ReadInt32();
                byte[] metaDataBytes = localReader.ReadBytes(metaBlockSize);

                var metaData = encoded ?
                                LZ4MessagePackSerializer.Deserialize<BlockMetadata>(metaDataBytes):
                                MessagePackSerializer.Deserialize<BlockMetadata>(metaDataBytes);

                retBlock = localReader.ReadBytes(metaData.BlockSize);
                typeID = metaData.TypeID;
                blockType = metaData.BlockType;
                timeStamp = metaData.TimeStamp;
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
                bufferFilled = true;
            }
        }

        public void RewindOneBlock()
        {
            lock (readerLock)
            {
                memoryStream.Position = lastBlockStartPosition;
            }
        }

        public void ResetToStart()
        {
            lock (readerLock)
            {
                memoryStream.Position = 0;
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
                    binaryReader.Dispose();
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

        public void WriteDataBlock(byte[] data, int typeID, uint blockType, long timeStamp, bool encode)
        {
            lock (writerLock)
            {
                //if (!isSignature)
                //{
                    var metaBlock = encode ? 
                                        LZ4MessagePackSerializer.Serialize(new BlockMetadata(typeID, timeStamp, data.Length, blockType)) :
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

    [MessagePackObject]
    public class Header
    {
        private Dictionary<Type, int> registrationIDs = new Dictionary<Type, int>();

        [Key(0)]
        public Dictionary<int, TypeRegistration> TypeRegistrations { get; set; } = new Dictionary<int, TypeRegistration>();

        [Key(1)]
        public uint MajorVersion { get; set; }

        [Key(2)]
        public uint MinorVersion { get; set; }

        [Key(3)]
        public uint Revision { get; set; }

        public Header()
        {

        }

        public void RegisterVersion(uint majorVersion, uint minorVersion, uint revision)
        {
            MajorVersion = majorVersion;
            MinorVersion = minorVersion;
            Revision = revision;
        }

        public void RegisterType(Type newType, BlockDataTypes outputType)
        {
            int id = TypeRegistrations.Count;
            TypeRegistrations.Add(id, new TypeRegistration(id, newType, outputType));
            registrationIDs.Add(newType, id);
        }

        public void ResetRegistrationIDs()
        {
            registrationIDs.Clear();

            try
            {
                foreach (var reg in TypeRegistrations)
                {
                    registrationIDs.Add(reg.Value.ClassType, reg.Key);
                }
            }
            catch(Exception ex)
            {

            }
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

        [Key(2)]
        public BlockDataTypes OutputType { get; set; }

        [IgnoreMember]
        public Type ClassType
        {
            get
            {
                if (linkedType == null)
                    linkedType = Type.GetType(LongName);
                return linkedType;
            }
        }

        public TypeRegistration()
        {

        }

        public TypeRegistration(int refID, Type newType, BlockDataTypes outputType)
        {
            linkedType = newType;

            RefID = refID;
            LongName = newType.AssemblyQualifiedName;
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
