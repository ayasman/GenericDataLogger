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

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestSerializeBufferWrite(bool encoded)
        {
            WriteDataBuffer sut = new WriteDataBuffer();
            MemoryStream ms = new MemoryStream();

            sut.WriteDataBlock(Guid.Empty.ToByteArray(), 0, 0, 0, encoded);

            sut.WriteTo(ms);
            Assert.NotEqual(0, ms.Length);
            var oldLength = ms.Length;

            sut.WriteTo(ms);
            Assert.Equal(oldLength, ms.Length);

            sut.Dispose();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestBadMemoryStreams(bool encoded)
        {
            MemoryStream ms = new MemoryStream();
            WriteDataBuffer sut = new WriteDataBuffer();
            sut.Dispose();

            Assert.False(sut.IsStreamOpen);
            Assert.Throws<SerializerException>(() => sut.WriteTo(ms));
            Assert.Throws<SerializerException>(() => sut.WriteDataBlock(Guid.Empty.ToByteArray(), 0, 0, 0, encoded));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestWritingHeader(bool encoded)
        {
            MemoryStream ms = new MemoryStream();

            CachedSerializeWriter writer = new CachedSerializeWriter(ms, encoded, false);
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

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestNoStream(bool encoded)
        {
            Stream ms = null;
            CachedSerializeWriter writer = new CachedSerializeWriter(ms, encoded, false);
            writer.RegisterType(typeof(TestData), BlockDataTypes.Full | BlockDataTypes.Partial);

            writer.WriteBuffer(0);

            Assert.Throws<SerializerException>(() => writer.FlushToStream());

            writer.Dispose();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestWritePartialFull(bool encoded)
        {
            MemoryStream ms = new MemoryStream();
            CachedSerializeWriter writer = new CachedSerializeWriter(ms, encoded, false);
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

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestBadWriter(bool encoded)
        {
            MemoryStream ms = new MemoryStream();
            CachedSerializeWriter sut = new CachedSerializeWriter(ms, encoded, false);
            sut.Dispose();

            Assert.Throws<SerializerException>(() => sut.FlushToStream());
            Assert.Throws<SerializerException>(() => sut.WriteBuffer(0));
            Assert.Throws<SerializerException>(() => sut.Write(0, fixture.Create<TestData>()));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestRegisterUnmarkedClass(bool encoded)
        {
            MemoryStream ms = new MemoryStream();
            CachedSerializeWriter sut = new CachedSerializeWriter(ms, encoded, false);

            Assert.Throws<SerializerException>(() => sut.RegisterType(typeof(UnmarkedTestData), BlockDataTypes.Full | BlockDataTypes.Partial));

            sut.Dispose();
        }
    }
}
