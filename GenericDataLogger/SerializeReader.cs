using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;

namespace AYLib.GenericDataLogger
{
    public class SerializeReader : IDisposable
    {
        static readonly MessagePackSerializerOptions lz4Options = MessagePackSerializerOptions.Standard.WithCompression(MessagePackCompression.Lz4BlockArray);

        private Subject<ReadSerializeData> onDataRead = new Subject<ReadSerializeData>();

        private readonly bool encoded = false;

        private Guid signature;
        private Header headerData;

        private Stream inputStream;
        private string inputFileName;

        private ReadDataBuffer dataBuffer = new ReadDataBuffer();

        public IObservable<ReadSerializeData> WhenDataRead => onDataRead.Publish().RefCount();

        public Guid Signature => signature;

        public Header HeaderData => headerData;

        public SerializeReader(Stream inputStream, bool encoded)
        {
            this.encoded = encoded;
            this.inputStream = inputStream;
        }

        public SerializeReader(string fileName, bool encoded)
        {
            this.encoded = encoded;
            Initialize(fileName);
        }

        public void Initialize(string fileName)
        {
            try
            {
                inputFileName = fileName;
                inputStream = new FileStream(inputFileName, FileMode.Open);
            }
            catch (Exception ex)
            {
                throw new Exception("Error initializing binary file stream.", ex);
            }
        }

        public void ReadFromStream()
        {
            try
            {
                if (dataBuffer == null)
                    throw new Exception("Read buffer not open.");
                if (inputStream == null)
                    throw new Exception("Cannot read from input stream, no input stream configured.");

                dataBuffer.ReadFrom(inputStream);
            }
            catch (Exception ex)
            {
                throw new Exception("Error reading from input stream.", ex);
            }
        }

        public void ReadHeader()
        {
            try
            {
                if (dataBuffer == null)
                    throw new Exception("Read buffer not open.");

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
                    var fileReader = new BinaryReader(inputStream, System.Text.Encoding.Default, true);
                    sigData = dataBuffer.ReadDataBlock(encoded, out int sigTypeID, out uint sigBlockType, out long sigTimeStamp, fileReader);
                    header = dataBuffer.ReadDataBlock(encoded, out int typeID, out uint blockType, out long timeStamp, fileReader);
                    fileReader.Dispose();
                }
                signature = new Guid(sigData);
                headerData = encoded ?
                    MessagePackSerializer.Deserialize<Header>(header, lz4Options) :
                    MessagePackSerializer.Deserialize<Header>(header, MessagePackSerializerOptions.Standard);
                headerData.ResetRegistrationIDs();
            }
            catch(Exception ex)
            {
                throw new Exception("Error reading header information.", ex);
            }
        }

        public void ReadData(long timeToReadTo = long.MaxValue)
        {
            try
            {
                if (dataBuffer == null)
                    throw new Exception("Read buffer not open.");

                var fileReader = new BinaryReader(inputStream, System.Text.Encoding.Default, true);

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
                        if (inputStream.Length == inputStream.Position)
                            break;
                        dataBlock = dataBuffer.ReadDataBlock(encoded, out typeID, out blockType, out timeStamp, fileReader);
                    }

                    if (timeToReadTo != long.MaxValue && timeToReadTo >= timeStamp)
                    {
                        dataBuffer.RewindOneBlock();
                        break;
                    }

                    var dataType = headerData.GetRegistrationType(typeID);
                    if (dataType != null)
                    {
                        var deserializedData = encoded ?
                                                    MessagePackSerializer.Deserialize(dataType, dataBlock, lz4Options) :
                                                    MessagePackSerializer.Deserialize(dataType, dataBlock);
                        onDataRead.OnNext(new ReadSerializeData(timeStamp, deserializedData, (BlockDataTypes)blockType));
                    }
                    else
                    {
                        var deserializedData = encoded ?
                                                    MessagePackSerializer.Typeless.Deserialize(dataBlock, lz4Options) :
                                                    MessagePackSerializer.Typeless.Deserialize(dataBlock);
                        onDataRead.OnNext(new ReadSerializeData(timeStamp, deserializedData, (BlockDataTypes)blockType));
                    }
                }

                fileReader.Dispose();
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
                    inputStream?.Dispose();
                    dataBuffer?.Dispose();
                    onDataRead?.Dispose();

                    inputStream = null;
                    dataBuffer = null;
                    onDataRead = null;
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
