// One rejoin attempt at a time: an attempt still waiting on Relay must not start a client once a
// newer attempt began or the reconnect was stopped (the session ended, the player quit).
public sealed class RejoinGate
{
    private int generation;

    public int Begin() => ++generation;

    public void Cancel() => generation++;

    public bool IsCurrent(int token) => token == generation;
}
