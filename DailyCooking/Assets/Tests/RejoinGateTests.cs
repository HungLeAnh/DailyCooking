using NUnit.Framework;

public class RejoinGateTests
{
    [Test] public void Begin_IsCurrent()
    {
        var gate = new RejoinGate();
        int token = gate.Begin();
        Assert.IsTrue(gate.IsCurrent(token));
    }

    [Test] public void Cancel_InvalidatesAttemptInFlight()
    {
        // A slow Relay join must not start a client after the session ended.
        var gate = new RejoinGate();
        int token = gate.Begin();
        gate.Cancel();
        Assert.IsFalse(gate.IsCurrent(token));
    }

    [Test] public void NewAttempt_InvalidatesOlderOne()
    {
        var gate = new RejoinGate();
        int first = gate.Begin();
        int second = gate.Begin();
        Assert.IsFalse(gate.IsCurrent(first));
        Assert.IsTrue(gate.IsCurrent(second));
    }
}
