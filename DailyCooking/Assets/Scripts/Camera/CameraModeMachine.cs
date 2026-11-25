using System;

public enum CameraMode
{
    Walk,
    CloseUp,
    Build,
}

// Which camera is live: walking, a close-up on one cooking tool, or the build-mode overhead.
// Pure state, no Unity types; CameraModeController applies it to the Cinemachine cameras.
public sealed class CameraModeMachine
{
    public CameraMode Mode { get; private set; } = CameraMode.Walk;
    public object CloseUpTarget { get; private set; }

    // Fires on every real change, including switching the close-up to another tool.
    public event Action<CameraMode> Changed;

    public bool EnterCloseUp(object target)
    {
        if (target == null || Mode == CameraMode.Build)
            return false;
        if (Mode == CameraMode.CloseUp && ReferenceEquals(CloseUpTarget, target))
            return true;
        Set(CameraMode.CloseUp, target);
        return true;
    }

    public void ExitCloseUp()
    {
        if (Mode == CameraMode.CloseUp)
            Set(CameraMode.Walk, null);
    }

    public void BuildStarted()
    {
        if (Mode != CameraMode.Build)
            Set(CameraMode.Build, null);
    }

    public void BuildEnded()
    {
        if (Mode == CameraMode.Build)
            Set(CameraMode.Walk, null);
    }

    public void ToolGone(object target)
    {
        if (Mode == CameraMode.CloseUp && ReferenceEquals(CloseUpTarget, target))
            Set(CameraMode.Walk, null);
    }

    private void Set(CameraMode mode, object target)
    {
        Mode = mode;
        CloseUpTarget = target;
        Changed?.Invoke(mode);
    }
}
