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
    }
}
