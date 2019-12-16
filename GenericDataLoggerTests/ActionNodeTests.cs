using AutoFixture;
using AYLib.GenericDataLogger;
using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace BehaviourTreeTests
{
    [MessagePackObject]
    public class TestData : IReplayData
    {
        [Key(0)]
        public Guid ReplayDataID { get; set; }

        [Key(1)]
        public int TestInt { get; set; }

        [Key(2)]
        public long TestLong { get; set; }

        [Key(3)]
        public double TestDouble { get; set; }

        [Key(4)]
        public string TestString { get; set; }

        public TestData()
        {

        }
    }

    internal class TestDataEqualityComparer : IEqualityComparer<TestData>
    {
        public bool Equals(TestData x, TestData y)
        {
            if (x.ReplayDataID == y.ReplayDataID &&
                x.TestInt == y.TestInt &&
                x.TestLong == y.TestLong &&
                x.TestDouble == y.TestDouble &&
                x.TestString == y.TestString)
                return true;
            return false;
        }

        public int GetHashCode(TestData obj)
        {
            return EqualityComparer<TestData>.Default.GetHashCode(obj);
        }

    }

    public class ActionNodeTests
    {
        Fixture fixture = new Fixture();
        List<TestData> initialTestData;

        public ActionNodeTests()
        {
            initialTestData = new List<TestData>();

            for(int i=0; i<10; i++)
            {
                initialTestData.Add(fixture.Create<TestData>());
            }
        }
    
        [Fact]
        public void TestWritingReadingFile()
        {
            ReplayWriter writer = new ReplayWriter(@"TestReplayOutput.rpy", true, false);
            writer.RegisterType(typeof(TestData), BlockDataTypes.Full | BlockDataTypes.Partial);
            writer.RegisterVersion(fixture.Create<uint>(), fixture.Create<uint>(), fixture.Create<uint>());

            foreach (var testData in initialTestData)
                writer.Update(testData);

            writer.WriteBuffer(0);
            writer.FlushToFile();
            writer.Dispose();


            var readTestData = new List<TestData>();

            ReplayReader reader = new ReplayReader(@"TestReplayOutput.rpy", true);

            reader.WhenDataRead.Subscribe(data =>
            {
                readTestData.Add(data.DataBlock as TestData);
            });

            reader.ReadFromFile();
            reader.ReadHeader();
            reader.ReadData();

            reader.Dispose();

            Assert.Equal(initialTestData, readTestData, new TestDataEqualityComparer());
            Assert.Equal(initialTestData.Count, readTestData.Count);
            Assert.Equal(ReplayWriter.Signature, reader.Signature);
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
