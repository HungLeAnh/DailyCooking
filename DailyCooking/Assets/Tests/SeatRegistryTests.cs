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
        Assert.AreEqual(SeatRegistry.ApprovalResult.Approved, s.Approve("new", 99, Max, 0f));
    }

    [Test] public void Full_NewPlayer_RoomFull()
    {
        var s = WithLive(4);
        Assert.AreEqual(SeatRegistry.ApprovalResult.RoomFull, s.Approve("new", 99, Max, 0f));
    }

    [Test] public void HeldSeat_CountsAsTaken()
    {
        var s = WithLive(4);
        s.Disconnected("p3", 3, 0f);
        Assert.IsTrue(s.IsHeld("p3"));
        Assert.AreEqual(4, s.TakenSeats);
        Assert.AreEqual(SeatRegistry.ApprovalResult.RoomFull, s.Approve("new", 99, Max, 0f));
    }

    [Test] public void KnownLive_ApprovedReplaceLive()
    {
        var s = WithLive(4);
        Assert.AreEqual(SeatRegistry.ApprovalResult.ApprovedReplaceLive, s.Approve("p2", 99, Max, 0f));
        Assert.AreEqual(2UL, s.LiveClient("p2"));
    }

    [Test] public void KnownHeld_ApprovedReclaimHeld()
    {
        var s = WithLive(4);
        s.Disconnected("p1", 1, 0f);
        Assert.AreEqual(SeatRegistry.ApprovalResult.ApprovedReclaimHeld, s.Approve("p1", 99, Max, 0f));
    }

    [Test] public void EmptyId_TreatedAsNewPlayer()
    {
        var s = WithLive(4);
        Assert.AreEqual(SeatRegistry.ApprovalResult.RoomFull, s.Approve("", 99, Max, 0f));
        Assert.AreEqual(SeatRegistry.ApprovalResult.Approved, WithLive(1).Approve(null, 99, Max, 0f));
    }

    [Test] public void Expire_After60s_ReleasesSeat()
    {
        var s = WithLive(4);
        s.Disconnected("p0", 0, 10f);
        CollectionAssert.AreEqual(new[] { "p0" }, s.Expire(70f));
        Assert.IsFalse(s.IsHeld("p0"));
        Assert.AreEqual(3, s.TakenSeats);
        Assert.AreEqual(SeatRegistry.ApprovalResult.Approved, s.Approve("new", 99, Max, 0f));
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

    [Test] public void Release_FreesHeldSeatAtOnce()
    {
        var s = WithLive(4);
        s.Disconnected("p2", 2, 0f);
        Assert.IsTrue(s.Release("p2"));
        Assert.IsFalse(s.IsHeld("p2"));
        Assert.AreEqual(3, s.TakenSeats);
        Assert.IsFalse(s.Release("p2"), "nothing left to release");
    }

    [Test] public void TwoPendingJoins_SecondRefusedWhenOneSeatLeft()
    {
        var s = WithLive(3);
        Assert.AreEqual(SeatRegistry.ApprovalResult.Approved, s.Approve("a", 7, Max, 0f));
        Assert.AreEqual(SeatRegistry.ApprovalResult.RoomFull, s.Approve("b", 8, Max, 0f));
    }

    [Test] public void PendingClearedOnConnect()
    {
        var s = WithLive(2);
        s.Approve("a", 7, Max, 0f);
        Assert.AreEqual(3, s.TakenSeats);
        s.Connected("a", 7);
        Assert.AreEqual(3, s.TakenSeats);
    }

    [Test] public void PendingClearedOnDropBeforeConnect()
    {
        var s = WithLive(3);
        s.Approve("a", 7, Max, 0f);
        s.DroppedBeforeConnect(7);
        Assert.AreEqual(3, s.TakenSeats);
        Assert.AreEqual(SeatRegistry.ApprovalResult.Approved, s.Approve("b", 8, Max, 1f));
    }

    [Test] public void PendingExpiresAfter30s()
    {
        var s = WithLive(3);
        s.Approve("a", 7, Max, 0f);
        CollectionAssert.IsEmpty(s.Expire(29.9f));
        Assert.AreEqual(4, s.TakenSeats);
        CollectionAssert.IsEmpty(s.Expire(30.1f));
        Assert.AreEqual(3, s.TakenSeats);
    }

    [Test] public void ReclaimHeld_DoesNotDoubleCount()
    {
        var s = WithLive(4);
        s.Disconnected("p1", 1, 0f);
        Assert.AreEqual(SeatRegistry.ApprovalResult.ApprovedReclaimHeld, s.Approve("p1", 9, Max, 1f));
        Assert.AreEqual(4, s.TakenSeats);
    }

    [Test] public void PendingWithoutId_ClearedOnConnect()
    {
        var s = WithLive(1);
        s.Approve(null, 7, Max, 0f);
        s.Connected(null, 7);
        Assert.AreEqual(1, s.TakenSeats);
    }

    [Test] public void Clear_EmptiesEverything()
    {
        var s = WithLive(3);
        s.Disconnected("p0", 0, 0f);
        s.Clear();
        Assert.AreEqual(0, s.TakenSeats);
    }
}
