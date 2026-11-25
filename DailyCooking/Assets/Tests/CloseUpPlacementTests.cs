using NUnit.Framework;
using UnityEngine;

public class CloseUpPlacementTests
{
    private static readonly Vector3 LookAt = new Vector3(0f, 1f, 0f);
    private static readonly Vector3 ToolForward = new Vector3(0f, 0f, 1f);
    private const float Distance = 2f;
    private const float Height = 1.5f;

    private static void AssertVector(Vector3 expected, Vector3 actual)
    {
        Assert.AreEqual(expected.x, actual.x, 0.001f, "x");
        Assert.AreEqual(expected.y, actual.y, 0.001f, "y");
        Assert.AreEqual(expected.z, actual.z, 0.001f, "z");
    }

    [Test] public void Unblocked_UsesPlayerSide()
    {
        CloseUpShot shot = CloseUpPlacement.Choose(LookAt, ToolForward, new Vector3(3f, 0f, 0f), Distance, Height, (a, b) => false);
        AssertVector(new Vector3(2f, 2.5f, 0f), shot.Position);
        Assert.IsFalse(shot.TopDown);
    }

    [Test] public void PlayerOnTool_UsesToolForward()
    {
        CloseUpShot shot = CloseUpPlacement.Choose(LookAt, ToolForward, Vector3.zero, Distance, Height, (a, b) => false);
        AssertVector(new Vector3(0f, 2.5f, 2f), shot.Position);
    }

    [Test] public void PreferredBlocked_UsesNearestToolSide()
    {
        CloseUpShot shot = CloseUpPlacement.Choose(LookAt, ToolForward, new Vector3(3f, 0f, 1f), Distance, Height,
            (from, to) => Mathf.Abs(from.z) > 0.1f);
        AssertVector(new Vector3(2f, 2.5f, 0f), shot.Position);
        Assert.IsFalse(shot.TopDown);
    }

    [Test] public void AllBlocked_GoesTopDown()
    {
        CloseUpShot shot = CloseUpPlacement.Choose(LookAt, ToolForward, new Vector3(3f, 0f, 0f), Distance, Height, (a, b) => true);
        AssertVector(new Vector3(0f, 4.5f, 0f), shot.Position);
        Assert.IsTrue(shot.TopDown);
    }

    [Test] public void IsBlocked_ReceivesLookAtAsTarget()
    {
        Vector3 received = Vector3.negativeInfinity;
        CloseUpPlacement.Choose(LookAt, ToolForward, new Vector3(3f, 0f, 0f), Distance, Height, (from, to) =>
        {
            received = to;
            return false;
        });
        AssertVector(LookAt, received);
    }
}
