using AutoFixture;
using AYLib.GenericDataLogger;
using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace GenericDataLoggerTests
{
    public class ReaderTests
    {
        private Fixture fixture = new Fixture();

        public ReaderTests()
        {

        }

        [Fact]
        public void TestSerializeBufferInit()
        {
            ReadDataBuffer sut = new ReadDataBuffer();

            Assert.True(sut.IsEndOfStream);

            sut.Dispose();
        }

        [Fact]
        public void TestSerializeBufferRead()
        {
            WriteDataBuffer writeBuf = new WriteDataBuffer();
            MemoryStream ms = new MemoryStream();
            writeBuf.WriteDataBlock(Guid.Empty.ToByteArray(), 1, 2, 3, false);
            writeBuf.WriteTo(ms);

            ReadDataBuffer sut = new ReadDataBuffer();
            sut.ReadFrom(ms);

            Assert.False(sut.IsEndOfStream);

            var data = sut.ReadDataBlock(false, out int typeID, out uint blockType, out long timeStamp);

            Assert.Equal(Guid.Empty.ToByteArray(), data);
            Assert.Equal(1, typeID);
            Assert.Equal((uint)2, blockType);
            Assert.Equal(3, timeStamp);
            Assert.True(sut.IsEndOfStream);
            sut.ResetToStart();
            Assert.False(sut.IsEndOfStream);

            sut.ReadDataBlock(false, out typeID, out blockType, out timeStamp);

            Assert.True(sut.IsEndOfStream);
            sut.RewindOneBlock();
            Assert.False(sut.IsEndOfStream);

            sut.Dispose();
            writeBuf.Dispose();
        }

        [Fact]
        public void TestSerializeBufferNull()
        {
            ReadDataBuffer sut = new ReadDataBuffer();
            Assert.Throws<Exception>(() => sut.ReadDataBlock(false, out int typeID, out uint blockType, out long timeStamp));
        }

        [Fact]
        public void TestBadMemoryStreams()
        {
            MemoryStream ms = new MemoryStream();
            ReadDataBuffer sut = new ReadDataBuffer();
            sut.Dispose();

            Assert.False(sut.IsStreamOpen);
            Assert.Throws<Exception>(() => sut.ResetToStart());
            Assert.Throws<Exception>(() => sut.RewindOneBlock());
            Assert.Throws<Exception>(() => sut.ReadFrom(ms));
            Assert.Throws<Exception>(() => sut.ReadDataBlock(false, out int typeID, out uint blockType, out long timeStamp));
        }

        [Fact]
        public void TestNoStream()
        {
            Stream ms = null;
            CachedSerializeReader sut = new CachedSerializeReader(ms, false);

            Assert.Throws<Exception>(() => sut.ReadHeader());
            Assert.Throws<Exception>(() => sut.ReadData());

            sut.Dispose();
        }

        [Fact]
        public void TestBadBuffer()
        {
            MemoryStream ms = new MemoryStream();
            CachedSerializeReader sut = new CachedSerializeReader(ms, false);
            sut.Dispose();

            Assert.Throws<Exception>(() => sut.ReadHeader());
            Assert.Throws<Exception>(() => sut.ReadData());
            Assert.Throws<Exception>(() => sut.ReadFromStream());
        }

        [Fact]
        public void TestReadHeader()
        {
            uint major = fixture.Create<uint>();
            uint minor = fixture.Create<uint>();
            uint rev = fixture.Create<uint>();

            MemoryStream ms = new MemoryStream();
            CachedSerializeWriter writer = new CachedSerializeWriter(ms, false, false);
            writer.RegisterType(typeof(TestData), BlockDataTypes.Full | BlockDataTypes.Partial);
            writer.RegisterVersion(major, minor, rev);
            writer.WriteBuffer(0);
            writer.FlushToStream();

            ms.Position = 0;

            CachedSerializeReader sut = new CachedSerializeReader(ms, false);
            sut.ReadHeader();
            sut.Dispose();
            writer.Dispose();

            Assert.Equal(Common.Signature, sut.Signature);
            Assert.Equal(major, sut.HeaderData.MajorVersion);
            Assert.Equal(minor, sut.HeaderData.MinorVersion);
            Assert.Equal(rev, sut.HeaderData.Revision);
        }

        [Fact]
        public void TestReadDataBlock()
        {
            var testData = fixture.Create<TestData>();
            MemoryStream ms = new MemoryStream();
            CachedSerializeWriter writer = new CachedSerializeWriter(ms, false, false);
            writer.RegisterType(typeof(TestData), BlockDataTypes.Full | BlockDataTypes.Partial);
            writer.RegisterVersion(fixture.Create<uint>(), fixture.Create<uint>(), fixture.Create<uint>());
            writer.Update(testData);
            writer.WriteBuffer(0);
            writer.FlushToStream();

            ms.Position = 0;

            CachedSerializeReader sut = new CachedSerializeReader(ms, false);

            sut.WhenDataRead.Subscribe(data =>
            {
                var readData = data.DataBlock as TestData;
                Assert.Equal(testData, readData, new TestDataEqualityComparer());
            });

            sut.ReadHeader();
            sut.ReadData();
            sut.Dispose();
            writer.Dispose();
        }


        [Fact]
        public void TestReadSingleDataBlock()
        {
            var testData = fixture.Create<TestData>();
            MemoryStream ms = new MemoryStream();
            CachedSerializeWriter writer = new CachedSerializeWriter(ms, false, false);
            writer.RegisterVersion(fixture.Create<uint>(), fixture.Create<uint>(), fixture.Create<uint>());
            writer.Write(0, testData);
            writer.FlushToStream();

            ms.Position = 0;

            CachedSerializeReader sut = new CachedSerializeReader(ms, false);

            sut.WhenDataRead.Subscribe(data =>
            {
                var readData = data.DataBlock as TestData;
                Assert.Equal(testData, readData, new TestDataEqualityComparer());
            });

            sut.ReadHeader();
            sut.ReadNextData(typeof(TestData));
            sut.Dispose();
            writer.Dispose();
        }
    }
}
