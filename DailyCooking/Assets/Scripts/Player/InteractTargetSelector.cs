using System.Collections.Generic;
using UnityEngine;

// Which in-range object the Interact button acts on: the one most directly in front of the
// character (on the ground plane); candidates within tieAngleDegrees of the best are tied and
// the nearest of those wins.
public static class InteractTargetSelector
{
    public static int SelectIndex(Vector3 playerPosition, Vector3 playerForward,
        IReadOnlyList<Vector3> candidatePositions, float tieAngleDegrees = 10f)
    {
        if (candidatePositions == null || candidatePositions.Count == 0)
            return -1;

        Vector3 forward = Flatten(playerForward);
        int count = candidatePositions.Count;
        var angles = new float[count];
        var distances = new float[count];
        float bestAngle = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            Vector3 offset = Flatten(candidatePositions[i] - playerPosition);
            distances[i] = offset.magnitude;
            angles[i] = offset.sqrMagnitude < 1e-6f ? 0f : Vector3.Angle(forward, offset);
            if (angles[i] < bestAngle)
                bestAngle = angles[i];
        }

        int best = -1;
        for (int i = 0; i < count; i++)
        {
            if (angles[i] > bestAngle + tieAngleDegrees)
                continue;
            if (best < 0 || distances[i] < distances[best])
                best = i;
        }
        return best;
    }

    private static Vector3 Flatten(Vector3 v)
    {
        v.y = 0f;
        return v;
    }
}
