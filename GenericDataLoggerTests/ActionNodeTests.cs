using AYLib.GenericDataLogger;
using MessagePack;
using System;
using System.IO;
using Xunit;

namespace BehaviourTreeTests
{
    [MessagePackObject]
    public class TestData : IReplayData
    {
        private Guid id = Guid.NewGuid();

        [IgnoreMember]
        public Guid ReplayDataID => id;

        [Key(0)]
        public int TestInt { get; set; }

        public TestData()
        {
            TestInt = 675;
        }
    }


    public class ActionNodeTests
    {
        [Fact]
        public void TestSuccess()
        {
            TestData data = new TestData();

            ReplayWriter writer = new ReplayWriter(@"TestReplayOutput.rpy", true);
            writer.RegisterType(typeof(TestData), BlockDataTypes.Full | BlockDataTypes.Partial);

            writer.Update(data);

            writer.WriteBuffer(0);

            writer.FlushToFile();

            writer.Dispose();









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

        //[Fact]
        //public void TestFailure()
        //{
        //    ActionNode testNode = new ActionNode("TestNode", (t, o) => BehaviourReturnCode.Failure);
        //    Assert.Equal(BehaviourReturnCode.Failure, testNode.Visit(1, null));
        //    Assert.Equal(BehaviourReturnCode.Failure, testNode.CurrentState);
        //}

        //[Fact]
        //public void TestRunning()
        //{
        //    ActionNode testNode = new ActionNode("TestNode", (t, o) => BehaviourReturnCode.Running);
        //    Assert.Equal(BehaviourReturnCode.Running, testNode.Visit(1, null));
        //    Assert.Equal(BehaviourReturnCode.Running, testNode.CurrentState);
        //}

        //[Fact]
        //public void TestError()
        //{
        //    ActionNode testNode = new ActionNode("TestNode", (t, o) => BehaviourReturnCode.Error);
        //    Assert.Equal(BehaviourReturnCode.Error, testNode.Visit(1, null));
        //    Assert.Equal(BehaviourReturnCode.Error, testNode.CurrentState);
        //}

        //[Fact]
        //public void TestErrorNoFunction()
        //{
        //    ActionNode testNode = new ActionNode("TestNode", null);
        //    Assert.Equal(BehaviourReturnCode.Error, testNode.Visit(1, null));
        //    Assert.Equal(BehaviourReturnCode.Error, testNode.CurrentState);
        //}

        //[Fact]
        //public void TestGetState()
        //{
        //    ActionNode testNode = new ActionNode("TestNode", (t, o) => BehaviourReturnCode.Success);
        //    testNode.Visit(1, null);

        //    var state = testNode.GetState();

        //    Assert.NotNull(state);
        //    Assert.Equal("TestNode", state.NodeName);
        //    Assert.Equal(BehaviourReturnCode.Success, state.CurrentState);
        //    Assert.Empty(state.Children);
        //}

        //[Fact]
        //public void TestStateReset()
        //{
        //    ActionNode testNode = new ActionNode("TestNode", (t, o) => BehaviourReturnCode.Success);
        //    testNode.Visit(1, null);
        //    testNode.ResetState();

        //    Assert.Equal(BehaviourReturnCode.Ready, testNode.CurrentState); 
        //}
    }
}
