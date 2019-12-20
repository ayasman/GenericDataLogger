using AutoFixture;
using AYLib.GenericDataLogger;
using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace GenericDataLoggerTests
{
    public class HeaderTests
    {
        private Fixture fixture = new Fixture();

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

            var outID = sut.GetRegistrationID(typeof(TestData));
            Assert.Equal(0, outID);

            var typeOut = sut.GetRegistrationType(0);
            Assert.NotNull(typeOut);
            Assert.Equal(typeof(TestData), typeOut);

            var blockOut = sut.GetRegistrationOutput(typeof(TestData));
            Assert.Equal(BlockDataTypes.Full, blockOut);
        }

        [Fact]
        public void TestNoTypeRegistration()
        {
            Header sut = new Header();

            Assert.Empty(sut.TypeRegistrations);

            var outID = sut.GetRegistrationID(typeof(TestData));
            Assert.Equal(-2, outID);

            var typeOut = sut.GetRegistrationType(0);
            Assert.Null(typeOut);

            var typeOut2 = sut.GetRegistrationType(-2);
            Assert.Null(typeOut2);

            var blockOut = sut.GetRegistrationOutput(typeof(TestData));
            Assert.Equal(BlockDataTypes.None, blockOut);
        }

        [Fact]
        public void TestTypeRegistrationReset()
        {
            Header sut = new Header();

            sut.TypeRegistrations.Add(0, new TypeRegistration(0, typeof(TestData), BlockDataTypes.Immediate));
            Assert.NotEmpty(sut.TypeRegistrations);

            var outID = sut.GetRegistrationID(typeof(TestData));
            Assert.Equal(-2, outID);

            sut.ResetRegistrationIDs();
            var outID2 = sut.GetRegistrationID(typeof(TestData));
            Assert.Equal(0, outID2);
        }
    }
}
