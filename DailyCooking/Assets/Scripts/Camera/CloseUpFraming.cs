using UnityEngine;

// Per-tool close-up framing: how far and how high the close-up camera sits from the tool and
// where it looks. Not networked; tools without it use CameraModeController's defaults.
public class CloseUpFraming : MonoBehaviour
{
    public const float DefaultDistance = 1.6f;
    public const float DefaultHeight = 1.4f;
    public const float DefaultFieldOfView = 48f;
    public static readonly Vector3 DefaultLookAtOffset = new Vector3(0f, 0.55f, 0f);

    [SerializeField] private float distance = DefaultDistance;
    [SerializeField] private float height = DefaultHeight;
    [SerializeField] private float fieldOfView = DefaultFieldOfView;
    [SerializeField] private Vector3 lookAtOffset = DefaultLookAtOffset;

    public float Distance => distance;
    public float Height => height;
    public float FieldOfView => fieldOfView;
    public Vector3 LookAt => transform.TransformPoint(lookAtOffset);
}
