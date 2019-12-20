using AutoFixture;
using AYLib.GenericDataLogger;
using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace GenericDataLoggerTests
{
    public class WriterTests
    {
        private Fixture fixture = new Fixture();

        public WriterTests()
        {

        }

        [Fact]
        public void TestSerializeBufferInit()
        {
            WriteDataBuffer sut = new WriteDataBuffer();
            MemoryStream ms = new MemoryStream();

            sut.WriteTo(ms);

            Assert.Equal(0, ms.Length);

            sut.Dispose();
        }

        [Fact]
        public void TestSerializeBufferWrite()
        {
            WriteDataBuffer sut = new WriteDataBuffer();
            MemoryStream ms = new MemoryStream();

            sut.WriteDataBlock(Guid.Empty.ToByteArray(), 0, 0, 0, false);

            sut.WriteTo(ms);
            Assert.NotEqual(0, ms.Length);
            var oldLength = ms.Length;

            sut.WriteTo(ms);
            Assert.Equal(oldLength, ms.Length);

            sut.Dispose();
        }

        [Fact]
        public void TestSerializeBufferWriteEncode()
        {
            WriteDataBuffer sut = new WriteDataBuffer();
            MemoryStream ms = new MemoryStream();

            sut.WriteDataBlock(Guid.Empty.ToByteArray(), 0, 0, 0, true);

            sut.WriteTo(ms);

            Assert.NotEqual(0, ms.Length);

            sut.Dispose();
        }

        [Fact]
        public void TestWritingHeader()
        {
            MemoryStream ms = new MemoryStream();

            SerializeWriter writer = new SerializeWriter(ms, false, false);
            writer.RegisterType(typeof(TestData), BlockDataTypes.Full | BlockDataTypes.Partial);

            writer.WriteBuffer(0);
            writer.FlushToStream();
            var lengthA = ms.Length;

            writer.WriteBuffer(0);
            writer.FlushToStream();
            var lengthB = ms.Length;

            writer.Dispose();

            Assert.NotEqual(0, lengthA);
            Assert.Equal(lengthA, lengthB);
        }

        [Fact]
        public void TestNoStream()
        {
            Stream ms = null;
            SerializeWriter writer = new SerializeWriter(ms, false, false);
            writer.RegisterType(typeof(TestData), BlockDataTypes.Full | BlockDataTypes.Partial);

            writer.WriteBuffer(0);

            Assert.Throws<Exception>(() => writer.FlushToStream());

            writer.Dispose();
        }

        [Fact]
        public void TestWritePartialFull()
        {
            MemoryStream ms = new MemoryStream();
            SerializeWriter writer = new SerializeWriter(ms, false, false);
            writer.RegisterType(typeof(TestData), BlockDataTypes.Full);
            writer.RegisterType(typeof(TestDataSmall), BlockDataTypes.Partial);
            writer.WriteBuffer(0);
            writer.FlushToStream();

            var initialLength = ms.Length;

            var largeDat = fixture.Create<TestData>();
            var smallDat = fixture.Create<TestDataSmall>();

            writer.Update(largeDat);
            writer.Update(smallDat);

            writer.WriteBuffer(1);
            writer.FlushToStream();
            var secondLength = ms.Length;

            writer.Update(largeDat);
            writer.Update(smallDat);

            writer.WriteBuffer(2, true);
            writer.FlushToStream();
            var lastLength = ms.Length;

            Assert.NotEqual(initialLength, secondLength);
            Assert.NotEqual(secondLength, lastLength);
            Assert.NotEqual(secondLength - initialLength, lastLength - secondLength);
        }
    }
}
