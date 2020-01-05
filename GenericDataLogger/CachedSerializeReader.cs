using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Text;

namespace AYLib.GenericDataLogger
{
    /// <summary>
    /// Class to handle the reading of cached writer output.
    /// 
    /// Data is pushed into the read buffer, then is read block by block until the end of the file
    /// or until a timestamp is reached.
    /// 
    /// Data block information is pushed to subscribed objects using the WhenDataRead observable.
    /// </summary>
    public class CachedSerializeReader : IDisposable
    {
        private Subject<ReadSerializeData> onDataRead = new Subject<ReadSerializeData>();

        private bool encoded = false;

        private Guid signature;
        private Header headerData;

        private Stream inputStream;
        private string inputFileName;

        private ReadDataBuffer dataBuffer = new ReadDataBuffer();

        /// <summary>
        /// Observable that notifies when data is read from the binary stream.
        /// </summary>
        public IObservable<ReadSerializeData> WhenDataRead => onDataRead.Publish().RefCount();

        /// <summary>
        /// Signature of the read binary stream.
        /// </summary>
        public Guid Signature => signature;

        /// <summary>
        /// Header data for the read binary stream.
        /// </summary>
        public Header HeaderData => headerData;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="inputStream">The stream to read from to fill the buffer on a ReadFromStream call</param>
        public CachedSerializeReader(Stream inputStream)
        {
            this.inputStream = inputStream;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="fileName">The name/location of the file to read from on a ReadFromStream call</param>
        public CachedSerializeReader(string fileName)
        {
            Initialize(fileName);
        }

        /// <summary>
        /// Sets the header data for the reader.
        /// </summary>
        /// <param name="newHeader">New header object</param>
        public void SetHeader(Header newHeader)
        {
            headerData = newHeader;
        }

        /// <summary>
        /// Initializes an input file for the ReadFromStream call.
        /// </summary>
        /// <param name="fileName">The name/location of the file to read from</param>
        public void Initialize(string fileName)
        {
            try
            {
                inputFileName = fileName;
                inputStream = new FileStream(inputFileName, FileMode.Open);
            }
            catch (Exception ex)
            {
                throw new StreamException("Error initializing binary file stream.", ex);
            }
        }

        /// <summary>
        /// Reads the data from the input stream (file or otherwise) to the data buffer.
        /// </summary>
        public void ReadFromStream()
        {
            try
            {
                if (dataBuffer == null)
                    throw new StreamException("Read buffer not open.");
                if (inputStream == null)
                    throw new StreamException("Cannot read from input stream, no input stream configured.");

                dataBuffer.ReadFrom(inputStream);
            }
            catch (Exception ex)
            {
                throw new SerializerException("Error reading from input stream.", ex);
            }
        }

        /// <summary>
        /// Reads the header object from the data buffer.
        /// </summary>
        public void ReadHeader()
        {
            try
            {
                if (dataBuffer == null)
                    throw new StreamException("Read buffer not open.");

                byte[] sigData = null;
                byte[] header = null;
                byte[] wasEncoded = null;
                if (dataBuffer.BufferFilled)
                {
                    dataBuffer.ResetToStart();
                    sigData = dataBuffer.ReadDataBlock(false, out int sigTypeID, out uint sigBlockType, out long sigTimeStamp);
                    wasEncoded = dataBuffer.ReadDataBlock(false, out int encTypeID, out uint encBlockType, out long encTimeStamp);

                    encoded = BitConverter.ToBoolean(wasEncoded, 0);

                    header = dataBuffer.ReadDataBlock(encoded, out int typeID, out uint blockType, out long timeStamp);
                }
                else
                {
                    var fileReader = new BinaryReader(inputStream, System.Text.Encoding.Default, true);
                    sigData = dataBuffer.ReadDataBlock(false, out int sigTypeID, out uint sigBlockType, out long sigTimeStamp, fileReader);
                    wasEncoded = dataBuffer.ReadDataBlock(false, out int encTypeID, out uint encBlockType, out long encTimeStamp, fileReader);

                    encoded = BitConverter.ToBoolean(wasEncoded, 0);

                    header = dataBuffer.ReadDataBlock(encoded, out int typeID, out uint blockType, out long timeStamp, fileReader);
                    fileReader.Dispose();
                }
                signature = new Guid(sigData);

                Header localHeader = SerializeProvider.DefaultProvider.Decode(true, encoded, typeof(Header), header) as Header;

                localHeader.ResetRegistrationIDs();

                if (headerData == null)
                    headerData = localHeader;
            }
            catch (Exception ex)
            {
                throw new SerializerException("Error reading header information.", ex);
            }
        }

        /// <summary>
        /// Reads a data block from the input stream, until EOF is hit or the timestamp to read to is found.
        /// </summary>
        /// <param name="timeToReadTo">Timestamp to read to, long.MaxValue for EOF</param>
        public void ReadData(long timeToReadTo = long.MaxValue)
        {
            try
            {
                if (dataBuffer == null)
                    throw new StreamException("Read buffer not open.");

                var fileReader = new BinaryReader(inputStream, System.Text.Encoding.Default, true);

                bool doRead = true;
                while (doRead)
                {
                    if (!DoReadNextData(fileReader, null, timeToReadTo))
                        break;
                }

                fileReader.Dispose();
            }
            catch (Exception ex)
            {
                throw new SerializerException("Error reading buffer information.", ex);
            }
        }

        /// <summary>
        /// Reads a single data block from the input stream.
        /// </summary>
        /// <param name="readType">The expected data type to read, or null to use a registered type</param>
        public void ReadNextData(Type readType = null)
        {
            try
            {
                if (dataBuffer == null)
                    throw new StreamException("Read buffer not open.");

                var fileReader = new BinaryReader(inputStream, System.Text.Encoding.Default, true);

                DoReadNextData(fileReader, readType);

                fileReader.Dispose();
            }
            catch (Exception ex)
            {
                throw new SerializerException("Error reading buffer information.", ex);
            }
        }

        /// <summary>
        /// Reads the next block of data from the buffer or a given stream. Read data is put on the
        /// observable stream for use by outside applications.
        /// </summary>
        /// <param name="reader">The stream to read from , if not using the data buffer</param>
        /// <param name="readType">Data type to read, null if using a registered type</param>
        /// <param name="timeToReadTo">Timestamp to read to, long.MaxValue for EOF</param>
        /// <returns></returns>
        private bool DoReadNextData(BinaryReader reader, Type readType = null, long timeToReadTo = long.MaxValue)
        {
            byte[] dataBlock = null;
            int typeID = -1;
            uint blockType;
            long timeStamp;

            if (dataBuffer.BufferFilled)
            {
                if (dataBuffer.IsEndOfStream)
                    return false;
                dataBlock = dataBuffer.ReadDataBlock(encoded, out typeID, out blockType, out timeStamp);
            }
            else
            {
                if (inputStream.Length == inputStream.Position)
                    return false;
                dataBlock = dataBuffer.ReadDataBlock(encoded, out typeID, out blockType, out timeStamp, reader);
            }

            if (timeToReadTo != long.MaxValue && timeToReadTo >= timeStamp)
            {
                dataBuffer.RewindOneBlock();
                return false;
            }

            var dataType = readType ?? headerData.GetRegistrationType(typeID);
            if (dataType != null)
            {
                var deserializedData = SerializeProvider.CurrentProvider.Decode(true, encoded, dataType, dataBlock);

                onDataRead.OnNext(new ReadSerializeData(timeStamp, deserializedData, (BlockDataTypes)blockType));
            }
            else
            {
                var deserializedData = SerializeProvider.CurrentProvider.Decode(false, encoded, dataType, dataBlock);

                onDataRead.OnNext(new ReadSerializeData(timeStamp, deserializedData, (BlockDataTypes)blockType));
            }
            return true;
        }

        /// <summary>
        /// Verify if the given version numbers are compatible with the read header.
        /// </summary>
        /// <param name="majorVersion">Major version number</param>
        /// <param name="minorVersion">Minor version number</param>
        /// <param name="revision">Revision number</param>
        /// <returns></returns>
        public bool Verify(uint majorVersion, uint minorVersion, uint revision)
        {
            return HeaderData?.Verify(majorVersion, minorVersion, revision) ?? false;
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
