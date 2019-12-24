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

        [Fact]
        public void TestDirectWriting()
        {
            MemoryStream ms = new MemoryStream();
            DirectSerializeWriter sut = new DirectSerializeWriter(false);

            sut.RegisterType(typeof(TestData));

            sut.Write(ms, fixture.Create<TestData>());

            Assert.NotEqual(0, ms.Length);

            Assert.Throws<Exception>(() => sut.Write(null, fixture.Create<TestData>()));
            Assert.Throws<Exception>(() => sut.Write(ms, fixture.Create<TestDataSmall>()));

            sut.Dispose();
            ms.Dispose();
        }

        [Fact]
        public void TestDirectReading()
        {
            MemoryStream msSmall = new MemoryStream();
            MemoryStream msLarge = new MemoryStream();

            DirectSerializeReader sut = new DirectSerializeReader(false);
            sut.RegisterType(typeof(TestData));

            DirectSerializeWriter writerSut = new DirectSerializeWriter(false);
            writerSut.RegisterType(typeof(TestData));
            writerSut.RegisterType(typeof(TestDataSmall));

            writerSut.Write(msLarge, fixture.Create<TestData>());
            writerSut.Write(msSmall, fixture.Create<TestDataSmall>());

            msSmall.Position = 0;
            msLarge.Position = 0;

            Assert.NotEqual(0, msSmall.Length);
            Assert.NotEqual(0, msLarge.Length);

            var retData = sut.Read(msLarge);

            Assert.Throws<Exception>(() => sut.Read(null));
            Assert.Throws<Exception>(() => sut.Read(msSmall));

            sut.Dispose();
            writerSut.Dispose();
            msSmall.Dispose();
            msLarge.Dispose();
        }
    }
}
