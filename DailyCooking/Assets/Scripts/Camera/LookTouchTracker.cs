using UnityEngine;

// Picks the one finger that turns the camera: the first touch that did not start on UI
// (so the joystick thumb never turns it), held until that finger lifts.
public sealed class LookTouchTracker
{
    private const int None = -1;

    private int lookTouchId = None;
    private Vector2 lastPosition;

    public bool HasLookFinger => lookTouchId != None;

    public void Began(int touchId, Vector2 position, bool overUI)
    {
        if (overUI || HasLookFinger)
            return;
        lookTouchId = touchId;
        lastPosition = position;
    }

    public Vector2 Moved(int touchId, Vector2 position)
    {
        if (touchId != lookTouchId)
            return Vector2.zero;
        Vector2 delta = position - lastPosition;
        lastPosition = position;
        return delta;
    }

    public void Ended(int touchId)
    {
        if (touchId == lookTouchId)
            lookTouchId = None;
    }
}
