using AutoFixture;
using AYLib.GenericDataLogger;
using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace GenericDataLoggerTests
{
    public class DirectReadWriteTests
    {
        private Fixture fixture = new Fixture();

        public DirectReadWriteTests()
        {

        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestDirectWriting(bool encoded)
        {
            MemoryStream ms = new MemoryStream();
            DirectSerializeWriter sut = new DirectSerializeWriter(encoded);

            sut.RegisterType(typeof(TestData));

            sut.Write(ms, fixture.Create<TestData>());

            Assert.NotEqual(0, ms.Length);

            Assert.Throws<SerializerException>(() => sut.Write(null, fixture.Create<TestData>()));
            Assert.Throws<SerializerException>(() => sut.Write(ms, fixture.Create<TestDataSmall>()));

            sut.Dispose();
            ms.Dispose();
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestDirectReading(bool encoded)
        {
            MemoryStream msSmall = new MemoryStream();
            MemoryStream msLarge = new MemoryStream();

            DirectSerializeReader sut = new DirectSerializeReader(encoded);
            sut.RegisterType(typeof(TestData));

            DirectSerializeWriter writerSut = new DirectSerializeWriter(encoded);
            writerSut.RegisterType(typeof(TestData));
            writerSut.RegisterType(typeof(TestDataSmall));

            TestData testData = fixture.Create<TestData>();

            writerSut.Write(msLarge, testData);
            writerSut.Write(msSmall, fixture.Create<TestDataSmall>());

            msSmall.Position = 0;
            msLarge.Position = 0;

            Assert.NotEqual(0, msSmall.Length);
            Assert.NotEqual(0, msLarge.Length);

            var retData = sut.Read(msLarge);

            Assert.Throws<SerializerException>(() => sut.Read(null));
            Assert.Throws<SerializerException>(() => sut.Read(msSmall));

            Assert.IsType<ReadSerializeData>(retData);
            Assert.Equal(testData, retData.DataBlock as TestData, new TestDataEqualityComparer());

            sut.Dispose();
            writerSut.Dispose();
            msSmall.Dispose();
            msLarge.Dispose();
        }
    }
}
