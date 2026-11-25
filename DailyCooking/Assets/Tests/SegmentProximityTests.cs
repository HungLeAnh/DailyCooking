using NUnit.Framework;
using UnityEngine;

public class SegmentProximityTests
{
    private static readonly Vector3 A = new Vector3(0f, 0f, 0f);
    private static readonly Vector3 B = new Vector3(0f, 0f, 10f);

    [Test] public void PointOnSegment_IsNear()
    {
        Assert.IsTrue(SegmentProximity.IsNear(A, B, new Vector3(0f, 0f, 4f), 0.01f));
    }

    [Test] public void PointBesideMiddle_UsesPerpendicularDistance()
    {
        Vector3 p = new Vector3(0.5f, 0f, 5f);
        Assert.IsTrue(SegmentProximity.IsNear(A, B, p, 0.6f));
        Assert.IsFalse(SegmentProximity.IsNear(A, B, p, 0.4f));
    }

    [Test] public void PointBeyondEnd_UsesEndpointDistance()
    {
        Vector3 p = new Vector3(0f, 0f, 11f);
        Assert.IsFalse(SegmentProximity.IsNear(A, B, p, 0.9f));
        Assert.IsTrue(SegmentProximity.IsNear(A, B, p, 1.1f));
    }

    [Test] public void DegenerateSegment_UsesPointDistance()
    {
        Vector3 p = new Vector3(0f, 1f, 0f);
        Assert.IsTrue(SegmentProximity.IsNear(A, A, p, 1.01f));
        Assert.IsFalse(SegmentProximity.IsNear(A, A, p, 0.99f));
    }
}
