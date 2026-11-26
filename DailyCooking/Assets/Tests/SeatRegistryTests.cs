using NUnit.Framework;

public class SeatRegistryTests
{
    private const int Max = 4;

    private static SeatRegistry WithLive(int count)
    {
        var s = new SeatRegistry();
        for (int i = 0; i < count; i++)
            s.Connected("p" + i, (ulong)i);
        return s;
    }

    [Test] public void NewPlayer_FreeSeat_Approved()
    {
        var s = WithLive(2);
        Assert.AreEqual(SeatRegistry.ApprovalResult.Approved, s.Approve("new", Max));
    }

    [Test] public void Full_NewPlayer_RoomFull()
    {
        var s = WithLive(4);
        Assert.AreEqual(SeatRegistry.ApprovalResult.RoomFull, s.Approve("new", Max));
    }

    [Test] public void HeldSeat_CountsAsTaken()
    {
        var s = WithLive(4);
        s.Disconnected("p3", 3, 0f);
        Assert.IsTrue(s.IsHeld("p3"));
        Assert.AreEqual(4, s.TakenSeats);
        Assert.AreEqual(SeatRegistry.ApprovalResult.RoomFull, s.Approve("new", Max));
    }

    [Test] public void KnownLive_ApprovedReplaceLive()
    {
        var s = WithLive(4);
        Assert.AreEqual(SeatRegistry.ApprovalResult.ApprovedReplaceLive, s.Approve("p2", Max));
        Assert.AreEqual(2UL, s.LiveClient("p2"));
    }

    [Test] public void KnownHeld_ApprovedReclaimHeld()
    {
        var s = WithLive(4);
        s.Disconnected("p1", 1, 0f);
        Assert.AreEqual(SeatRegistry.ApprovalResult.ApprovedReclaimHeld, s.Approve("p1", Max));
    }

    [Test] public void EmptyId_TreatedAsNewPlayer()
    {
        var s = WithLive(4);
        Assert.AreEqual(SeatRegistry.ApprovalResult.RoomFull, s.Approve("", Max));
        Assert.AreEqual(SeatRegistry.ApprovalResult.Approved, WithLive(1).Approve(null, Max));
    }

    [Test] public void Expire_After60s_ReleasesSeat()
    {
        var s = WithLive(4);
        s.Disconnected("p0", 0, 10f);
        CollectionAssert.AreEqual(new[] { "p0" }, s.Expire(70f));
        Assert.IsFalse(s.IsHeld("p0"));
        Assert.AreEqual(3, s.TakenSeats);
        Assert.AreEqual(SeatRegistry.ApprovalResult.Approved, s.Approve("new", Max));
    }

    [Test] public void Expire_Before60s_Keeps()
    {
        var s = WithLive(2);
        s.Disconnected("p0", 0, 10f);
        CollectionAssert.IsEmpty(s.Expire(69.9f));
        Assert.IsTrue(s.IsHeld("p0"));
    }

    [Test] public void Reconnect_ClearsHold()
    {
        var s = WithLive(2);
        s.Disconnected("p0", 0, 0f);
        s.Connected("p0", 7);
        Assert.IsFalse(s.IsHeld("p0"));
        Assert.AreEqual(7UL, s.LiveClient("p0"));
        Assert.AreEqual(2, s.TakenSeats);
    }

    [Test] public void ReplacedConnection_OldDisconnect_DoesNotHoldSeat()
    {
        var s = WithLive(2);
        s.Connected("p1", 9); // the same player on a new connection
        s.Disconnected("p1", 1, 0f); // the old connection closing
        Assert.IsFalse(s.IsHeld("p1"));
        Assert.AreEqual(9UL, s.LiveClient("p1"));
    }

    [Test] public void Clear_EmptiesEverything()
    {
        var s = WithLive(3);
        s.Disconnected("p0", 0, 0f);
        s.Clear();
        Assert.AreEqual(0, s.TakenSeats);
    }
}
