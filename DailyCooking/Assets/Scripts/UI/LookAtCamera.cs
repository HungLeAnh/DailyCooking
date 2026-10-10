using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    private enum Mode
    {
        LookAt,
        LookAtInverted,
        CameraForward,
        CameraForwardInverted,
    }
    [SerializeField] private Mode mode;

    private Camera _mainCamera;

    // Camera.main re-searches by tag on every read; this runs every frame.
    private Camera MainCamera => _mainCamera != null ? _mainCamera : _mainCamera = Camera.main;

    private void LateUpdate()
    {
        Camera cam = MainCamera;
        switch (mode)
        {
            case Mode.LookAt:
                transform.LookAt(cam.transform);
                break;

            case Mode.LookAtInverted:
                Vector3 dirFromCamera = transform.position - cam.transform.position;
                transform.LookAt(transform.position + dirFromCamera);
                break;
            case Mode.CameraForward:
                transform.forward = cam.transform.forward;
                break;
            case Mode.CameraForwardInverted:
                transform.forward = -cam.transform.forward;

                break;
        }
    }
}
