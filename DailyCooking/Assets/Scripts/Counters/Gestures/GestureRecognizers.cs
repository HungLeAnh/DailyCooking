using UnityEngine;

// Pure recognisers for the close-up gestures, fed with screen-space pointer positions.
public static class GestureRecognizers
{
    private const float StirTriggerDegrees = 300f;

    // How centred a swipe crosses the food: 1 through the middle, 0 at its edge (radius),
    // -1 when it misses the food or is shorter than the food's radius.
    public static float SwipeCentredness(Vector2 start, Vector2 end, Vector2 centre, float radius)
    {
        if (radius <= 0f)
            return -1f;
        Vector2 segment = end - start;
        float length = segment.magnitude;
        if (length < radius)
            return -1f;
        float t = Mathf.Clamp01(Vector2.Dot(centre - start, segment) / (length * length));
        float distance = Vector2.Distance(centre, start + segment * t);
        if (distance > radius)
            return -1f;
        return 1f - distance / radius;
    }

    // Fill level for a hold: 1 = full after fillSeconds; may exceed 1 (overflow).
    public static float PourLevel(float holdSeconds, float fillSeconds)
    {
        return fillSeconds <= 0f ? 1f : holdSeconds / fillSeconds;
    }

    // Counts one stir each time the pointer has gone most of the way around the centre.
    public sealed class StirTracker
    {
        private Vector2 centre;
        private Vector2? last;
        private float accumulatedDegrees;

        public void Reset(Vector2 stirCentre)
        {
            centre = stirCentre;
            last = null;
            accumulatedDegrees = 0f;
        }

        public bool Feed(Vector2 position)
        {
            if (last.HasValue)
                accumulatedDegrees += Vector2.SignedAngle(last.Value - centre, position - centre);
            last = position;
            if (Mathf.Abs(accumulatedDegrees) < StirTriggerDegrees)
                return false;
            accumulatedDegrees = 0f;
            return true;
        }
    }
}
