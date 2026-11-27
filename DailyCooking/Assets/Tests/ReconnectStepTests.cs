using NUnit.Framework;

public class ReconnectStepTests
{
    // Next(state, listening, connected, shutdownInProgress, attemptInFlight, secondsSinceAttempt)

    [Test] public void Reconnecting_NotListening_StartsAttempt() =>
        Assert.AreEqual(ReconnectAction.StartAttempt, ReconnectStep.Next(SessionState.Reconnecting, false, false, false, false, 99f));

    [Test] public void Listening_NotYetConnected_Waits() =>
        // A rejoin still handshaking / loading GameScene must not be shut down.
        Assert.AreEqual(ReconnectAction.Wait, ReconnectStep.Next(SessionState.Reconnecting, true, false, false, false, 3f));

    [Test] public void ConnectedWithoutSnapshot_DropsStaleConnection() =>
        // Connected (scene synced) but the snapshot timed out: back in Reconnecting while still connected.
        Assert.AreEqual(ReconnectAction.DropStaleConnection, ReconnectStep.Next(SessionState.Reconnecting, true, true, false, false, 16f));

    [Test] public void HungHandshake_DroppedAfter20s() =>
        Assert.AreEqual(ReconnectAction.DropStaleConnection, ReconnectStep.Next(SessionState.Reconnecting, true, false, false, false, 20.5f));

    [Test] public void AttemptInFlight_Waits() =>
        Assert.AreEqual(ReconnectAction.Wait, ReconnectStep.Next(SessionState.Reconnecting, false, false, false, true, 0f));

    [Test] public void ShutdownInProgress_Waits() =>
        Assert.AreEqual(ReconnectAction.Wait, ReconnectStep.Next(SessionState.Reconnecting, true, true, true, false, 30f));

    [Test] public void Syncing_Waits() =>
        Assert.AreEqual(ReconnectAction.Wait, ReconnectStep.Next(SessionState.Syncing, true, true, false, false, 30f));
}
