using AutoFixture;
using AYLib.GenericDataLogger;
using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace GenericDataLoggerTests
{
    public class FileReadWriteTests
    {
        private string testOutputFile = @"TestReplayOutput.rpy";
        private Fixture fixture = new Fixture();
        private List<TestData> initialTestData;

        public FileReadWriteTests()
        {
            initialTestData = new List<TestData>();

            for(int i=0; i<10; i++)
            {
                initialTestData.Add(fixture.Create<TestData>());
            }
        }
    
        [Fact]
        public void TestWritingReadingFileEncoded()
        {
            CachedSerializeWriter writer = new CachedSerializeWriter(testOutputFile, true, false);
            writer.RegisterType(typeof(TestData), BlockDataTypes.Full | BlockDataTypes.Partial);
            writer.RegisterVersion(fixture.Create<uint>(), fixture.Create<uint>(), fixture.Create<uint>());

            foreach (var testData in initialTestData)
                writer.Update(testData);

            writer.WriteBuffer(0);
            writer.FlushToStream();
            writer.Dispose();

            var readTestData = new List<TestData>();

            CachedSerializeReader reader = new CachedSerializeReader(testOutputFile, true);

            reader.WhenDataRead.Subscribe(data =>
            {
                readTestData.Add(data.DataBlock as TestData);
            });

            reader.ReadFromStream();
            reader.ReadHeader();
            reader.ReadData();

            reader.Dispose();

            Assert.Equal(initialTestData, readTestData, new TestDataEqualityComparer());
            Assert.Equal(initialTestData.Count, readTestData.Count);
            Assert.Equal(Common.Signature, reader.Signature);
            Assert.Equal(writer.HeaderData.MajorVersion, reader.HeaderData.MajorVersion);
            Assert.Equal(writer.HeaderData.MinorVersion, reader.HeaderData.MinorVersion);
            Assert.Equal(writer.HeaderData.Revision, reader.HeaderData.Revision);
        }
    }
}
