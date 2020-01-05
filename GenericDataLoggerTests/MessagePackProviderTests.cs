using AutoFixture;
using AYLib.GenericDataLogger;
using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace GenericDataLoggerTests
{
    public class MessagePackProviderTests
    {
        private Fixture fixture = new Fixture();

        public MessagePackProviderTests()
        {

        }

        [Fact]
        public void TestDefaultProviders()
        {
            Assert.NotNull(SerializeProvider.CurrentProvider);
            Assert.NotNull(SerializeProvider.DefaultProvider);
            Assert.Equal(SerializeProvider.DefaultProvider, SerializeProvider.CurrentProvider);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void TestSerializeDeserialize(bool encoded)
        {
            TestData testData = fixture.Create<TestData>();

            var encodedData = SerializeProvider.DefaultProvider.Encode(true, encoded, typeof(TestData), testData);
            TestData decodedData = SerializeProvider.DefaultProvider.Decode(true, encoded, typeof(TestData), encodedData) as TestData;

            Assert.Equal(testData, decodedData, new TestDataEqualityComparer());
        }

        [Fact]
        public void TestValidCheck()
        {
            Assert.True(SerializeProvider.DefaultProvider.IsTypeValid(typeof(TestData)));
            Assert.False(SerializeProvider.DefaultProvider.IsTypeValid(typeof(UnmarkedTestData)));
        }
    }
}
