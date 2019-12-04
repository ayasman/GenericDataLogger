using MessagePack;
using System;
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
    public class Class1 : IDisposable
    {
        private WriteDataBuffer writeBuffer = new WriteDataBuffer();

        private Guid Signature = Guid.Parse("46429DF1-46C8-4C0D-8479-A3BCB6A87643");

        private Header headerData = new Header();

        public Class1()
        {
            //Signature.ToByteArray();

            

            //headerData.TypeRegistrations.Add(0, new TypeRegistration(0, typeof(Class1)));
            //headerData.TypeRegistrations.Add(1, new TypeRegistration(1, typeof(Class1)));
            //headerData.TypeRegistrations.Add(2, new TypeRegistration(2, typeof(Class1)));

            //var data = MessagePackSerializer.Serialize(typeList);
            //var back = MessagePackSerializer.Deserialize<Dictionary<string, string>>(data);

            //var bin = MessagePackSerializer.Typeless.Serialize(typeList);
            //var objModel = MessagePackSerializer.Typeless.Deserialize(bin) as Dictionary<string, string>;

            try
            {
                //TypeRegistration newReg = new TypeRegistration(0, typeof(Class1));
                //var data = MessagePackSerializer.Serialize(headerData);
                //var back = MessagePackSerializer.Deserialize<Header>(data);

                //using (var fileStream = new FileStream(@"TestFile.dat", FileMode.Append, FileAccess.Write, FileShare.None))
                //{
                //    CreateHeader(fileStream);
                //}

                //using (var memStream = new MemoryStream())
                //{
                //    CreateHeader(memStream);
                //}
            }
            catch (Exception ex)
            {

            }
        }

        public void RegisterType(Type newType)
        {
            headerData.RegisterType(newType);
        }

        public void CreateHeader(Stream targetStream)
        {
            WriteDataBlock(targetStream, Signature.ToByteArray(), true);
            WriteDataBlock(targetStream, MessagePackSerializer.Serialize(headerData), false);
        }

        public void WriteDataBlock(Stream targetStream, byte[] data, bool isSignature = false)
        {
            using (var bWriter = new BinaryWriter(targetStream, System.Text.Encoding.Default, true))
            {
                if (!isSignature)
                    bWriter.Write(data.Length);
                bWriter.Write(data);
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
                    writeBuffer.Dispose();
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

        public WriteDataBuffer()
        {
            memoryStream = new MemoryStream();
            binaryWriter = new BinaryWriter(memoryStream, System.Text.Encoding.Default, true);
        }

        public void WriteDataBlock(byte[] data, bool isSignature = false)
        {
            if (!isSignature)
                binaryWriter.Write(data.Length);
            binaryWriter.Write(data);
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
