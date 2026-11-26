using NUnit.Framework;

public class SnapshotAssemblerTests
{
    private static readonly byte[] A = { 1 };
    private static readonly byte[] B = { 2 };
    private static readonly byte[] C = { 3 };

    [Test] public void SingleChunk_Completes()
    {
        var s = new SnapshotAssembler();
        Assert.IsTrue(s.Add(1, 0, 1, A, out byte[][] done));
        Assert.AreEqual(1, done.Length);
        Assert.AreSame(A, done[0]);
    }

    [Test] public void OutOfOrder_CompletesWhenAllArrive()
    {
        var s = new SnapshotAssembler();
        Assert.IsFalse(s.Add(1, 1, 2, B, out _));
        Assert.IsTrue(s.Add(1, 0, 2, A, out byte[][] done));
        Assert.AreSame(A, done[0]);
        Assert.AreSame(B, done[1]);
    }

    [Test] public void NewSnapshotId_DiscardsOldChunks()
    {
        var s = new SnapshotAssembler();
        Assert.IsFalse(s.Add(1, 0, 3, A, out _));
        Assert.IsFalse(s.Add(1, 1, 3, B, out _));
        Assert.IsTrue(s.Add(2, 0, 1, C, out byte[][] done));
        Assert.AreEqual(1, done.Length);
        Assert.AreSame(C, done[0]);
        // A late chunk of the old snapshot starts its own partial set and never completes with id 2's data.
        Assert.IsFalse(s.Add(1, 2, 3, A, out _));
    }

    [Test] public void BadIndex_Ignored()
    {
        var s = new SnapshotAssembler();
        Assert.IsFalse(s.Add(1, 5, 2, A, out _));
        Assert.IsFalse(s.Add(1, -1, 2, A, out _));
        Assert.IsFalse(s.Add(1, 0, 0, A, out _));
        Assert.IsFalse(s.Add(1, 0, 1, null, out _));
    }

    [Test] public void Reset_DropsPartial()
    {
        var s = new SnapshotAssembler();
        Assert.IsFalse(s.Add(1, 0, 2, A, out _));
        s.Reset();
        Assert.IsFalse(s.Add(1, 1, 2, B, out _));
    }
}
