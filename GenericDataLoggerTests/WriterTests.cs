using AutoFixture;
using AYLib.GenericDataLogger;
using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace BehaviourTreeTests
{
    public class WriterTests
    {
        private Fixture fixture = new Fixture();
        private List<TestData> initialTestData;

        public WriterTests()
        {

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
            writer.FlushToStream();

            writer.Dispose();
        }
    }
}
