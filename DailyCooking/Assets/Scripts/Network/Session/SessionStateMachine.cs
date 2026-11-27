using System;

public enum SessionState { Idle, Joining, Syncing, InGame, Reconnecting, Hosting, Ended }

public enum SessionEndReason { None, Quit, HostClosed, ConnectionLost, HostConnectionLost, RoomFull, JoinFailed, Replaced }

// Where this machine's network session is, and why it ended. Pure: MultiplayerManager feeds it
// transport/handshake events and runs the single exit when it reaches Ended.
public sealed class SessionStateMachine
{
    public SessionState State { get; private set; } = SessionState.Idle;
    public SessionEndReason EndReason { get; private set; } = SessionEndReason.None;
    public event Action<SessionState> OnChanged;

    private bool hostClosing;
    private bool rejoining;

    public void StartHosting()
    {
        if (State == SessionState.Idle) Go(SessionState.Hosting);
    }

    public void StartJoining()
    {
        if (State == SessionState.Idle) Go(SessionState.Joining);
    }

    public void Connected()
    {
        if (State == SessionState.Joining) Go(SessionState.Syncing);
    }

    public void SnapshotApplied()
    {
        if (State != SessionState.Syncing) return;
        rejoining = false;
        Go(SessionState.InGame);
    }

    // The host announced it is closing: the next loss is "closed", not "lost".
    public void HostClosing()
    {
        if (State != SessionState.Ended && State != SessionState.Hosting) hostClosing = true;
    }

    public void TransportLost()
    {
        switch (State)
        {
            case SessionState.Hosting:
                End(SessionEndReason.HostConnectionLost);
                break;
            case SessionState.Joining:
                End(hostClosing ? SessionEndReason.HostClosed : SessionEndReason.JoinFailed);
                break;
            case SessionState.Syncing:
                if (hostClosing) End(SessionEndReason.HostClosed);
                else if (rejoining) Go(SessionState.Reconnecting);
                else End(SessionEndReason.JoinFailed);
                break;
            case SessionState.InGame:
                if (hostClosing) End(SessionEndReason.HostClosed);
                else Go(SessionState.Reconnecting);
                break;
            // Reconnecting: a failed attempt; keep retrying until the window is over.
        }
    }

    public void Refused(SessionEndReason reason)
    {
        if (State == SessionState.Joining || State == SessionState.Syncing || State == SessionState.Reconnecting)
            End(reason);
    }

    // The host ended this client's connection on purpose (e.g. the same player joined from another
    // device): end at once, never retry.
    public void Kicked(SessionEndReason reason)
    {
        if (State == SessionState.Joining || State == SessionState.Syncing
            || State == SessionState.InGame || State == SessionState.Reconnecting)
            End(reason);
    }

    public void RejoinSucceeded()
    {
        if (State != SessionState.Reconnecting) return;
        rejoining = true;
        Go(SessionState.Syncing);
    }

    public void RetryWindowOver()
    {
        if (State == SessionState.Reconnecting) End(SessionEndReason.ConnectionLost);
    }

    public void SnapshotTimedOut()
    {
        if (State != SessionState.Syncing) return;
        if (rejoining) Go(SessionState.Reconnecting);
        else End(SessionEndReason.JoinFailed);
    }

    public void UserQuit()
    {
        if (State != SessionState.Idle) End(SessionEndReason.Quit);
    }

    public void Reset()
    {
        State = SessionState.Idle;
        EndReason = SessionEndReason.None;
        hostClosing = false;
        rejoining = false;
    }

    private void End(SessionEndReason reason)
    {
        if (State == SessionState.Ended) return;
        EndReason = reason;
        Go(SessionState.Ended);
    }

    private void Go(SessionState next)
    {
        if (State == next) return;
        State = next;
        OnChanged?.Invoke(next);
    }
}
