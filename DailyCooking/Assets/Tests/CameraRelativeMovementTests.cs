using NUnit.Framework;
using UnityEngine;

public class CameraRelativeMovementTests
{
    private static void AssertVector(Vector3 expected, Vector3 actual)
    {
        Assert.AreEqual(expected.x, actual.x, 0.001f, "x");
        Assert.AreEqual(expected.y, actual.y, 0.001f, "y");
        Assert.AreEqual(expected.z, actual.z, 0.001f, "z");
    }

    [Test] public void Yaw0_UpIsPlusZ()
    {
        AssertVector(new Vector3(0f, 0f, 1f), CameraRelativeMovement.ToWorld(new Vector2(0f, 1f), 0f));
    }

    [Test] public void Yaw90_UpIsPlusX()
    {
        AssertVector(new Vector3(1f, 0f, 0f), CameraRelativeMovement.ToWorld(new Vector2(0f, 1f), 90f));
    }

    [Test] public void Yaw90_RightIsMinusZ()
    {
        AssertVector(new Vector3(0f, 0f, -1f), CameraRelativeMovement.ToWorld(new Vector2(1f, 0f), 90f));
    }

    [Test] public void Zero_StaysZero()
    {
        AssertVector(Vector3.zero, CameraRelativeMovement.ToWorld(Vector2.zero, 37f));
    }

    [Test] public void Length_IsPreserved()
    {
        Assert.AreEqual(1f, CameraRelativeMovement.ToWorld(new Vector2(0.6f, 0.8f), 37f).magnitude, 0.001f);
    }
}
