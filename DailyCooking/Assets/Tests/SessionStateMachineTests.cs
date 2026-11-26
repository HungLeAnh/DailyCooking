using System.Collections.Generic;
using NUnit.Framework;

public class SessionStateMachineTests
{
    private static SessionStateMachine InGame()
    {
        var m = new SessionStateMachine();
        m.StartJoining();
        m.Connected();
        m.SnapshotApplied();
        return m;
    }

    [Test] public void Join_Connect_Snapshot_InGame()
    {
        var m = new SessionStateMachine();
        Assert.AreEqual(SessionState.Idle, m.State);
        m.StartJoining();
        Assert.AreEqual(SessionState.Joining, m.State);
        m.Connected();
        Assert.AreEqual(SessionState.Syncing, m.State);
        m.SnapshotApplied();
        Assert.AreEqual(SessionState.InGame, m.State);
    }

    [Test] public void TransportLost_InGame_Reconnecting()
    {
        var m = InGame();
        m.TransportLost();
        Assert.AreEqual(SessionState.Reconnecting, m.State);
    }

    [Test] public void HostClosing_ThenLost_EndsHostClosed()
    {
        var m = InGame();
        m.HostClosing();
        m.TransportLost();
        Assert.AreEqual(SessionState.Ended, m.State);
        Assert.AreEqual(SessionEndReason.HostClosed, m.EndReason);
    }

    [Test] public void Reconnecting_Rejoin_Syncing()
    {
        var m = InGame();
        m.TransportLost();
        m.RejoinSucceeded();
        Assert.AreEqual(SessionState.Syncing, m.State);
        m.SnapshotApplied();
        Assert.AreEqual(SessionState.InGame, m.State);
    }

    [Test] public void Reconnecting_WindowOver_EndsConnectionLost()
    {
        var m = InGame();
        m.TransportLost();
        m.TransportLost(); // a failed attempt keeps retrying
        Assert.AreEqual(SessionState.Reconnecting, m.State);
        m.RetryWindowOver();
        Assert.AreEqual(SessionState.Ended, m.State);
        Assert.AreEqual(SessionEndReason.ConnectionLost, m.EndReason);
    }

    [Test] public void Joining_Refused_EndsWithReason()
    {
        var m = new SessionStateMachine();
        m.StartJoining();
        m.Refused(SessionEndReason.RoomFull);
        Assert.AreEqual(SessionState.Ended, m.State);
        Assert.AreEqual(SessionEndReason.RoomFull, m.EndReason);
    }

    [Test] public void Reconnecting_RoomFull_EndsRoomFull()
    {
        var m = InGame();
        m.TransportLost();
        m.Refused(SessionEndReason.RoomFull);
        Assert.AreEqual(SessionEndReason.RoomFull, m.EndReason);
    }

    [Test] public void Syncing_SnapshotTimeout_EndsJoinFailed()
    {
        var m = new SessionStateMachine();
        m.StartJoining();
        m.Connected();
        m.SnapshotTimedOut();
        Assert.AreEqual(SessionState.Ended, m.State);
        Assert.AreEqual(SessionEndReason.JoinFailed, m.EndReason);
    }

    [Test] public void RejoinSyncing_Lost_BackToReconnecting()
    {
        var m = InGame();
        m.TransportLost();
        m.RejoinSucceeded();
        m.TransportLost();
        Assert.AreEqual(SessionState.Reconnecting, m.State);
    }

    [Test] public void Hosting_TransportLost_EndsHostConnectionLost()
    {
        var m = new SessionStateMachine();
        m.StartHosting();
        Assert.AreEqual(SessionState.Hosting, m.State);
        m.TransportLost();
        Assert.AreEqual(SessionState.Ended, m.State);
        Assert.AreEqual(SessionEndReason.HostConnectionLost, m.EndReason);
    }

    [Test] public void UserQuit_AnyState_EndsQuit()
    {
        var host = new SessionStateMachine();
        host.StartHosting();
        host.UserQuit();
        Assert.AreEqual(SessionEndReason.Quit, host.EndReason);

        var reconnecting = InGame();
        reconnecting.TransportLost();
        reconnecting.UserQuit();
        Assert.AreEqual(SessionEndReason.Quit, reconnecting.EndReason);
    }

    [Test] public void TransportLost_WhileJoining_EndsJoinFailed()
    {
        var m = new SessionStateMachine();
        m.StartJoining();
        m.TransportLost();
        Assert.AreEqual(SessionState.Ended, m.State);
        Assert.AreEqual(SessionEndReason.JoinFailed, m.EndReason);
    }

    [Test] public void Ended_IgnoresFurtherInputs()
    {
        var m = InGame();
        m.UserQuit();
        m.TransportLost();
        m.RetryWindowOver();
        m.RejoinSucceeded();
        Assert.AreEqual(SessionState.Ended, m.State);
        Assert.AreEqual(SessionEndReason.Quit, m.EndReason);
        m.Reset();
        Assert.AreEqual(SessionState.Idle, m.State);
        Assert.AreEqual(SessionEndReason.None, m.EndReason);
    }

    [Test] public void OnChanged_FiresOncePerTransition()
    {
        var m = new SessionStateMachine();
        var seen = new List<SessionState>();
        m.OnChanged += s => seen.Add(s);
        m.StartJoining();
        m.Connected();
        m.Connected(); // no transition
        m.SnapshotApplied();
        m.UserQuit();
        CollectionAssert.AreEqual(new[] { SessionState.Joining, SessionState.Syncing, SessionState.InGame, SessionState.Ended }, seen);
    }
}
