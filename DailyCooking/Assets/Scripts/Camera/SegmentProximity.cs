using UnityEngine;

public static class SegmentProximity
{
    // True when point lies within radius of the segment a-b (clamped to its ends).
    public static bool IsNear(Vector3 a, Vector3 b, Vector3 point, float radius)
    {
        Vector3 ab = b - a;
        float lengthSquared = ab.sqrMagnitude;
        float t = lengthSquared < 1e-8f ? 0f : Mathf.Clamp01(Vector3.Dot(point - a, ab) / lengthSquared);
        Vector3 closest = a + ab * t;
        return (point - closest).sqrMagnitude <= radius * radius;
    }
}
