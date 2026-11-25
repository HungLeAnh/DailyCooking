using System.Collections.Generic;
using NUnit.Framework;

public class CameraModeMachineTests
{
    private CameraModeMachine machine;
    private List<CameraMode> changes;
    private readonly object t1 = new object();
    private readonly object t2 = new object();

    [SetUp]
    public void SetUp()
    {
        machine = new CameraModeMachine();
        changes = new List<CameraMode>();
        machine.Changed += mode => changes.Add(mode);
    }

    [Test] public void Starts_InWalk()
    {
        Assert.AreEqual(CameraMode.Walk, machine.Mode);
        Assert.IsNull(machine.CloseUpTarget);
    }

    [Test] public void EnterCloseUp_FromWalk_GoesCloseUp()
    {
        Assert.IsTrue(machine.EnterCloseUp(t1));
        Assert.AreEqual(CameraMode.CloseUp, machine.Mode);
        Assert.AreSame(t1, machine.CloseUpTarget);
        CollectionAssert.AreEqual(new[] { CameraMode.CloseUp }, changes);
    }

    [Test] public void EnterCloseUp_Null_IsIgnored()
    {
        Assert.IsFalse(machine.EnterCloseUp(null));
        Assert.AreEqual(CameraMode.Walk, machine.Mode);
        Assert.IsEmpty(changes);
    }

    [Test] public void EnterCloseUp_DuringBuild_IsIgnored()
    {
        machine.BuildStarted();
        Assert.IsFalse(machine.EnterCloseUp(t1));
        Assert.AreEqual(CameraMode.Build, machine.Mode);
    }

    [Test] public void EnterCloseUp_OtherTarget_SwitchesTarget()
    {
        machine.EnterCloseUp(t1);
        machine.EnterCloseUp(t2);
        Assert.AreSame(t2, machine.CloseUpTarget);
        Assert.AreEqual(2, changes.Count);
    }

    [Test] public void EnterCloseUp_SameTarget_NoEvent()
    {
        machine.EnterCloseUp(t1);
        machine.EnterCloseUp(t1);
        Assert.AreEqual(1, changes.Count);
    }

    [Test] public void ExitCloseUp_GoesWalk()
    {
        machine.EnterCloseUp(t1);
        machine.ExitCloseUp();
        Assert.AreEqual(CameraMode.Walk, machine.Mode);
        Assert.IsNull(machine.CloseUpTarget);
    }

    [Test] public void ToolGone_CurrentTarget_GoesWalk()
    {
        machine.EnterCloseUp(t1);
        machine.ToolGone(t1);
        Assert.AreEqual(CameraMode.Walk, machine.Mode);
        Assert.IsNull(machine.CloseUpTarget);
    }

    [Test] public void ToolGone_OtherTarget_StaysCloseUp()
    {
        machine.EnterCloseUp(t1);
        machine.ToolGone(t2);
        Assert.AreEqual(CameraMode.CloseUp, machine.Mode);
        Assert.AreSame(t1, machine.CloseUpTarget);
    }

    [Test] public void BuildStarted_FromCloseUp_ClearsTarget()
    {
        machine.EnterCloseUp(t1);
        machine.BuildStarted();
        Assert.AreEqual(CameraMode.Build, machine.Mode);
        Assert.IsNull(machine.CloseUpTarget);
    }

    [Test] public void BuildEnded_AfterCloseUp_GoesToWalk()
    {
        machine.EnterCloseUp(t1);
        machine.BuildStarted();
        machine.BuildEnded();
        Assert.AreEqual(CameraMode.Walk, machine.Mode);
        Assert.IsNull(machine.CloseUpTarget);
    }
}
