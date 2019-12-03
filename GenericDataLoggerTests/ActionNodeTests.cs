using AYLib.GenericDataLogger;
using System;
using Xunit;

namespace BehaviourTreeTests
{
    public class ActionNodeTests
    {
        [Fact]
        public void TestSuccess()
        {
            Class1 dfsafds = new Class1();

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
