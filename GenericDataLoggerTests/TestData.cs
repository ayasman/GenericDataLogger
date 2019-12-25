using AutoFixture;
using AYLib.GenericDataLogger;
using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace GenericDataLoggerTests
{
    [MessagePackObject]
    public class TestData : ISerializeData
    {
        [Key(0)]
        public Guid SerializeDataID { get; set; }

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

    [MessagePackObject]
    public class TestDataSmall : ISerializeData
    {
        [Key(0)]
        public Guid SerializeDataID { get; set; }

        [Key(1)]
        public int TestInt { get; set; }

        public TestDataSmall()
        {

        }
    }


    public class UnmarkedTestData : ISerializeData
    {
        public Guid SerializeDataID { get; set; }

        public int TestInt { get; set; }

        public UnmarkedTestData()
        {

        }
    }

    internal class TestDataEqualityComparer : IEqualityComparer<TestData>
    {
        public bool Equals(TestData x, TestData y)
        {
            if (x.SerializeDataID == y.SerializeDataID &&
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
}
