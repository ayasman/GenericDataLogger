using MessagePack;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;

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
    /// </summary>
    public class DataLogger : IDisposable
    {
        protected Guid Signature = Guid.Parse("46429DF1-46C8-4C0D-8479-A3BCB6A87643");

        public virtual Header HeaderData => null;

        public DataLogger()
        {

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

    public class DataLoggerWriter : DataLogger
    {
        private WriteDataBuffer dataBuffer = new WriteDataBuffer();
        private Header headerData = new Header();
        private ConcurrentQueue<byte[]> outputBuffer = new ConcurrentQueue<byte[]>();

        public override Header HeaderData => headerData;

        public DataLoggerWriter() :
            base()
        {

        }

        public void RegisterType(Type newType)
        {
            headerData.RegisterType(newType);
        }

        public void CreateHeader()
        {
            dataBuffer.WriteDataBlock(Signature.ToByteArray(), true);
            dataBuffer.WriteDataBlock(MessagePackSerializer.Serialize(headerData));
        }

        public void WriteData(byte[] data)
        {
            outputBuffer.Enqueue(data);
        }

        public void FlushBuffer()
        {
            byte[] data;
            while (outputBuffer.TryDequeue(out data))
                dataBuffer.WriteDataBlock(data);
        }

        public void WriteTo(Stream target)
        {
            dataBuffer.WriteTo(target);
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    dataBuffer.Dispose();
                }

                disposedValue = true;
            }
            base.Dispose();
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

        public void WriteDataBlock(byte[] data, bool isSignature = false)
        {
            lock (writerLock)
            {
                if (!isSignature)
                    binaryWriter.Write(data.Length);
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
        [Key(0)]
        public Dictionary<int, TypeRegistration> TypeRegistrations { get; set; }

        public Header()
        {
            TypeRegistrations = new Dictionary<int, TypeRegistration>();
        }

        public void RegisterType(Type newType)
        {
            int id = TypeRegistrations.Count;
            TypeRegistrations.Add(id, new TypeRegistration(id, newType));
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

        public TypeRegistration()
        {

        }

        public TypeRegistration(int refID, Type newType)
        {
            linkedType = newType;

            RefID = refID;
            LongName = newType.FullName;
        }
    }

    internal class DataBlock
    {

    }
}
