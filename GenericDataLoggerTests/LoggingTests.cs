using AutoFixture;
using AYLib.GenericDataLogger;
using Divergic.Logging.Xunit;
using MessagePack;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace GenericDataLoggerTests
{
    public class LoggingTests
    {
        private Fixture fixture = new Fixture();

        public LoggingTests()
        {

        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void VerifyDebugLogHeader(bool encoded)
        {
            var logger = new CacheLogger();

            MemoryStream ms = new MemoryStream();

            CachedSerializeWriter writer = new CachedSerializeWriter(ms, encoded, false, logger);
            writer.RegisterType(typeof(TestData), BlockDataTypes.Full | BlockDataTypes.Partial);

            writer.WriteBuffer(0);
            writer.FlushToStream();

            Assert.NotEmpty(logger.Entries);
            Assert.Equal(5, logger.Count);
            Assert.True(logger.Entries.All(p => p.LogLevel == LogLevel.Debug));

            writer.Dispose();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void VerifyDebugLogWrite(bool encoded)
        {
            var logger = new CacheLogger();
            var testData = fixture.Create<TestData>();
            MemoryStream ms = new MemoryStream();
            CachedSerializeWriter writer = new CachedSerializeWriter(ms, encoded, false, logger);
            writer.RegisterType(typeof(TestData), BlockDataTypes.Full | BlockDataTypes.Partial);
            writer.RegisterVersion(fixture.Create<uint>(), fixture.Create<uint>(), fixture.Create<uint>());
            writer.Update(testData);
            writer.WriteBuffer(0);
            writer.FlushToStream();

            Assert.NotEmpty(logger.Entries);
            Assert.Equal(6, logger.Count);
            Assert.True(logger.Entries.All(p => p.LogLevel == LogLevel.Debug));

            writer.Dispose();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void VerifyDebugLogWriteNoCache(bool encoded)
        {
            var logger = new CacheLogger();
            var testData = fixture.Create<TestData>();
            MemoryStream ms = new MemoryStream();
            CachedSerializeWriter writer = new CachedSerializeWriter(ms, encoded, false, logger);
            writer.RegisterVersion(fixture.Create<uint>(), fixture.Create<uint>(), fixture.Create<uint>());
            writer.Write(0, testData);
            writer.FlushToStream();

            Assert.NotEmpty(logger.Entries);
            Assert.Equal(6, logger.Count);
            Assert.True(logger.Entries.All(p => p.LogLevel == LogLevel.Debug));

            writer.Dispose();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void VerifyDebugReadHeader(bool encoded)
        {
            MemoryStream ms = new MemoryStream();
            CachedSerializeWriter writer = new CachedSerializeWriter(ms, encoded, false);
            writer.RegisterType(typeof(TestData), BlockDataTypes.Full | BlockDataTypes.Partial);
            writer.RegisterVersion(fixture.Create<uint>(), fixture.Create<uint>(), fixture.Create<uint>());
            writer.WriteBuffer(0);
            writer.FlushToStream();

            ms.Position = 0;

            var logger = new CacheLogger();
            CachedSerializeReader sut = new CachedSerializeReader(ms, logger);
            sut.ReadHeader();
            sut.Dispose();
            writer.Dispose();

            Assert.NotEmpty(logger.Entries);
            Assert.Equal(4, logger.Count);
            Assert.True(logger.Entries.All(p => p.LogLevel == LogLevel.Debug));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void VerifyDebugReadData(bool encoded)
        {
            var testData = fixture.Create<TestData>();
            MemoryStream ms = new MemoryStream();
            CachedSerializeWriter writer = new CachedSerializeWriter(ms, encoded, false);
            writer.RegisterVersion(fixture.Create<uint>(), fixture.Create<uint>(), fixture.Create<uint>());
            writer.Write(0, testData);
            writer.FlushToStream();

            ms.Position = 0;

            var logger = new CacheLogger();
            CachedSerializeReader sut = new CachedSerializeReader(ms, logger);

            sut.WhenDataRead.Subscribe(data =>
                {
                    var readData = data.DataBlock as TestData;
                });

            sut.ReadHeader();
            sut.ReadNextData(typeof(TestData));
            sut.Dispose();
            writer.Dispose();

            Assert.NotEmpty(logger.Entries);
            Assert.Equal(5, logger.Count);
            Assert.True(logger.Entries.All(p => p.LogLevel == LogLevel.Debug));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void VerifyDebugDirectWriter(bool encoded)
        {
            var logger = new CacheLogger();
            MemoryStream ms = new MemoryStream();
            DirectSerializeWriter sut = new DirectSerializeWriter(encoded, logger);

            sut.RegisterType(typeof(TestData));

            sut.Write(ms, fixture.Create<TestData>());

            sut.Dispose();
            ms.Dispose();

            Assert.NotEmpty(logger.Entries);
            Assert.Equal(1, logger.Count);
            Assert.True(logger.Entries.All(p => p.LogLevel == LogLevel.Debug));
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void VerifyDebugDirectReader(bool encoded)
        {
            var logger = new CacheLogger();
            MemoryStream ms = new MemoryStream();
            DirectSerializeWriter writer = new DirectSerializeWriter(encoded);
            writer.RegisterType(typeof(TestData));
            writer.Write(ms, fixture.Create<TestData>());

            ms.Position = 0;

            DirectSerializeReader sut = new DirectSerializeReader(encoded, logger);
            sut.RegisterType(typeof(TestData));
            var retData = sut.Read(ms);

            writer.Dispose();
            sut.Dispose();
            ms.Dispose();

            Assert.NotEmpty(logger.Entries);
            Assert.Equal(1, logger.Count);
            Assert.True(logger.Entries.All(p => p.LogLevel == LogLevel.Debug));
        }
    }
}
