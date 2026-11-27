public enum ReconnectAction { Wait, StartAttempt, DropStaleConnection }

// What the reconnect loop does on each tick. An attempt that is still handshaking or loading
// GameScene is left alone; only a connection that finished connecting but never got its snapshot
// (back in Reconnecting while connected), or a handshake stuck for 20 s, is dropped to retry.
public static class ReconnectStep
{
    public const float HandshakeLimitSeconds = 20f;

    public static ReconnectAction Next(SessionState state, bool listening, bool connected,
        bool shutdownInProgress, bool attemptInFlight, float secondsSinceAttempt)
    {
        if (state != SessionState.Reconnecting || attemptInFlight || shutdownInProgress)
            return ReconnectAction.Wait;
        if (!listening)
            return ReconnectAction.StartAttempt;
        return connected || secondsSinceAttempt > HandshakeLimitSeconds
            ? ReconnectAction.DropStaleConnection
            : ReconnectAction.Wait;
    }
}
