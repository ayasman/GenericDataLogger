using AutoFixture;
using AYLib.GenericDataLogger;
using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace BehaviourTreeTests
{
    public class HeaderTests
    {
        private Fixture fixture = new Fixture();
        private List<TestData> initialTestData;

        public HeaderTests()
        {

        }

        [Fact]
        public void TestVersionSetting()
        {
            uint major = fixture.Create<uint>();
            uint minor = fixture.Create<uint>();
            uint revision = fixture.Create<uint>();

            Header sut = new Header();
            sut.RegisterVersion(major, minor, revision);

            Assert.Equal(major, sut.MajorVersion);
            Assert.Equal(minor, sut.MinorVersion);
            Assert.Equal(revision, sut.Revision);
        }

        [Fact]
        public void TestTypeRegistration()
        {
            Header sut = new Header();
            sut.RegisterType(typeof(TestData), BlockDataTypes.Full);

            Assert.NotEmpty(sut.TypeRegistrations);
            Assert.Equal(typeof(TestData), sut.TypeRegistrations[0].ClassType);
            Assert.Equal(BlockDataTypes.Full, sut.TypeRegistrations[0].OutputType);
        }
    }
}
