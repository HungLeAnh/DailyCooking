using NUnit.Framework;

public class ReconnectStepTests
{
    [Test] public void Reconnecting_NotListening_StartsAttempt() =>
        Assert.AreEqual(ReconnectAction.StartAttempt, ReconnectStep.Next(SessionState.Reconnecting, false, false, false));

    [Test] public void ListeningWhileReconnecting_DropsStaleConnection() =>
        // A rejoin that connected but never got its snapshot is back in Reconnecting while still connected.
        Assert.AreEqual(ReconnectAction.DropStaleConnection, ReconnectStep.Next(SessionState.Reconnecting, true, false, false));

    [Test] public void AttemptInFlight_Waits() =>
        Assert.AreEqual(ReconnectAction.Wait, ReconnectStep.Next(SessionState.Reconnecting, false, false, true));

    [Test] public void ShutdownInProgress_Waits() =>
        Assert.AreEqual(ReconnectAction.Wait, ReconnectStep.Next(SessionState.Reconnecting, true, true, false));

    [Test] public void Syncing_Waits() =>
        Assert.AreEqual(ReconnectAction.Wait, ReconnectStep.Next(SessionState.Syncing, true, false, false));
}
