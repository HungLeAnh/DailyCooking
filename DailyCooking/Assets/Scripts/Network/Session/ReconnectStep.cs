public enum ReconnectAction { Wait, StartAttempt, DropStaleConnection }

// What the reconnect loop does on each tick. A rejoin that connected but never got its snapshot is
// back in Reconnecting while still connected: drop that connection so the next tick can retry.
public static class ReconnectStep
{
    public static ReconnectAction Next(SessionState state, bool listening, bool shutdownInProgress, bool attemptInFlight)
    {
        if (state != SessionState.Reconnecting || attemptInFlight || shutdownInProgress)
            return ReconnectAction.Wait;
        return listening ? ReconnectAction.DropStaleConnection : ReconnectAction.StartAttempt;
    }
}
