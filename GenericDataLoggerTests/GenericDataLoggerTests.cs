using AutoFixture;
using AYLib.GenericDataLogger;
using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace GenericDataLoggerTests
{
    /// <summary>
    /// To Test:
    /// File Create
    /// No Stream to write to
    /// Stream written to
    /// Invalid types
    /// Invalid data
    /// </summary>
    public class GenericDataLoggerTests
    {
        private string testOutputFile = @"TestReplayOutput.rpy";
        private Fixture fixture = new Fixture();
        private List<TestData> initialTestData;

        public GenericDataLoggerTests()
        {
            initialTestData = new List<TestData>();

            for(int i=0; i<10; i++)
            {
                initialTestData.Add(fixture.Create<TestData>());
            }
        }
    
        //[Fact]
        public void TestWritingReadingFileEncoded()
        {
            SerializeWriter writer = new SerializeWriter(testOutputFile, true, false);
            writer.RegisterType(typeof(TestData), BlockDataTypes.Full | BlockDataTypes.Partial);
            writer.RegisterVersion(fixture.Create<uint>(), fixture.Create<uint>(), fixture.Create<uint>());

            foreach (var testData in initialTestData)
                writer.Update(testData);

            writer.WriteBuffer(0);
            writer.FlushToStream();
            writer.Dispose();


            var readTestData = new List<TestData>();

            SerializeReader reader = new SerializeReader(testOutputFile, true);

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

        //[Fact]
        public void TestSuccess()
        {
            //ReplayReader reader = new ReplayReader(@"TestReplayOutput.rpy", true);
            ////reader.ReadFromFile();
            //reader.ReadHeader();
            //reader.ReadFromFile();
            //reader.ReadHeader();


            //TestData data = new TestData();

            //ReplayWriter writer = new ReplayWriter(@"TestReplayOutput.rpy", true, false);
            //writer.RegisterType(typeof(TestData), BlockDataTypes.Full | BlockDataTypes.Partial);

            //writer.Update(data);

            //writer.WriteBuffer(0);

            //writer.FlushToFile();

            //writer.Dispose();









            //DataLoggerWriter writer = new DataLoggerWriter();
            //writer.RegisterType(typeof(TestData));

            //writer.CreateHeader();

            //writer.WriteData(MessagePackSerializer.Serialize(new TestData()));
            //writer.FlushBuffer();

            ////var mem = new MemoryStream();
            //var mem = new FileStream(@"OutputTest.bin", FileMode.Create);

            //writer.WriteTo(mem);

            //writer.WriteData(MessagePackSerializer.Serialize(new TestData()));

            //writer.FlushBuffer();

            //writer.WriteTo(mem);

            //mem.Dispose();
        }
    }
}
