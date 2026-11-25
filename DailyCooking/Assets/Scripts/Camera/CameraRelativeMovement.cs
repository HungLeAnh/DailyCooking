using UnityEngine;

// Joystick input turned into a world direction relative to the camera's facing:
// "up" on the stick walks where the camera looks. Yaw 0 keeps today's world axes (up = +Z).
public static class CameraRelativeMovement
{
    public static Vector3 ToWorld(Vector2 input, float cameraYawDegrees)
    {
        return Quaternion.Euler(0f, cameraYawDegrees, 0f) * new Vector3(input.x, 0f, input.y);
    }
}
