using System;
using System.Collections.Generic;
using UnityEngine;

public struct CloseUpShot
{
    public Vector3 Position;
    public bool TopDown;
}

// Where the close-up camera goes for a cooking tool. Tools can face any of 4 directions (build-mode
// rotation), so the shot is taken from the side the player stands on, falling back to the tool's
// other sides and finally straight down when walls block every side.
public static class CloseUpPlacement
{
    private const float MinDirection = 0.01f;

    public static CloseUpShot Choose(Vector3 lookAt, Vector3 toolForward, Vector3 playerPosition,
        float distance, float height, Func<Vector3, Vector3, bool> isBlocked)
    {
        Vector3 forward = Flatten(toolForward);
        if (forward.sqrMagnitude < MinDirection * MinDirection)
            forward = Vector3.forward;

        Vector3 preferred = Flatten(playerPosition - lookAt);
        preferred = preferred.sqrMagnitude < MinDirection * MinDirection ? forward : preferred.normalized;

        Vector3 right = Vector3.Cross(Vector3.up, forward);
        var sides = new List<Vector3> { forward, right, -forward, -right };
        sides.Sort((a, b) => Vector3.Angle(preferred, a).CompareTo(Vector3.Angle(preferred, b)));
        sides.Insert(0, preferred);

        foreach (Vector3 direction in sides)
        {
            Vector3 position = lookAt + direction * distance + Vector3.up * height;
            if (!isBlocked(position, lookAt))
                return new CloseUpShot { Position = position, TopDown = false };
        }

        return new CloseUpShot { Position = lookAt + Vector3.up * (height + distance), TopDown = true };
    }

    private static Vector3 Flatten(Vector3 v)
    {
        v.y = 0f;
        return v.sqrMagnitude < MinDirection * MinDirection ? Vector3.zero : v.normalized;
    }
}
