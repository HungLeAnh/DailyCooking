using System;
using Unity.Cinemachine;
using UnityEngine;

// Local, non-networked: picks which Cinemachine camera is live (Walk, CloseUp on one cooking tool,
// or Build) and lets the CinemachineBrain blend between them. Each player only moves their own view.
public class CameraModeController : Singleton<CameraModeController>
{
    private const int LivePriority = 20;
    private const int IdlePriority = 0;
    private const float LineOfSightToolMargin = 0.6f;

    [SerializeField] private CinemachineCamera walkCamera;
    [SerializeField] private CinemachineCamera closeUpCamera;
    [SerializeField] private CinemachineCamera buildCamera;
    [SerializeField] private LayerMask closeUpBlockers;
    [SerializeField] private float lookDegreesPerPixel = 0.2f;

    private readonly CameraModeMachine machine = new CameraModeMachine();
    private CinemachineOrbitalFollow walkOrbit;
    private IBuildingPlacementManager subscribedPlacement;

    public event Action<CameraMode> OnModeChanged;

    public CameraMode Mode => machine.Mode;
    public float WalkYaw => walkCamera != null ? walkCamera.transform.eulerAngles.y : 0f;
    public CookingTool CloseUpTool => machine.CloseUpTarget as CookingTool;
    public Vector3 CloseUpLookAt { get; private set; }
    public Transform CloseUpCameraTransform => closeUpCamera != null ? closeUpCamera.transform : null;

    protected override void Awake()
    {
        base.Awake();
        if (walkCamera != null)
            walkOrbit = walkCamera.GetComponent<CinemachineOrbitalFollow>();
        machine.Changed += Machine_Changed;
        ApplyPriorities();
    }

    private void Start()
    {
        if (GameInput.Instance != null)
            GameInput.Instance.OnLookDelta += GameInput_OnLookDelta;
        if (GridBuildingSystem.Instance != null)
        {
            if (GridBuildingSystem.Instance.IsInitialized)
                SubscribePlacement();
            else
                GridBuildingSystem.Instance.OnObjectSpawned += SubscribePlacement;
        }
    }

    private void OnDestroy()
    {
        machine.Changed -= Machine_Changed;
        if (GameInput.Instance != null)
            GameInput.Instance.OnLookDelta -= GameInput_OnLookDelta;
        if (GridBuildingSystem.Instance != null)
            GridBuildingSystem.Instance.OnObjectSpawned -= SubscribePlacement;
        if (subscribedPlacement != null)
        {
            subscribedPlacement.OnBuildingStart -= Placement_OnBuildingStart;
            subscribedPlacement.OnBuildingEnd -= Placement_OnBuildingEnd;
        }
    }

    private void Update()
    {
        if (machine.Mode == CameraMode.CloseUp)
        {
            CookingTool tool = CloseUpTool;
            if (tool == null || !tool.IsSpawned)
                machine.ToolGone(machine.CloseUpTarget);
        }

        PlayerStateMachine localPlayer = PlayerStateMachine.LocalInstance;
        if (walkCamera != null && localPlayer != null && walkCamera.Follow != localPlayer.transform)
        {
            walkCamera.Follow = localPlayer.transform;
            walkCamera.LookAt = localPlayer.transform;
        }
    }

    public bool RequestCloseUp(CookingTool tool)
    {
        PlayerStateMachine localPlayer = PlayerStateMachine.LocalInstance;
        if (tool == null || closeUpCamera == null || localPlayer == null || machine.Mode == CameraMode.Build)
            return false;

        CloseUpFraming framing = tool.GetComponentInChildren<CloseUpFraming>();
        Vector3 lookAt = framing != null ? framing.LookAt : tool.transform.TransformPoint(CloseUpFraming.DefaultLookAtOffset);
        float distance = framing != null ? framing.Distance : CloseUpFraming.DefaultDistance;
        float height = framing != null ? framing.Height : CloseUpFraming.DefaultHeight;
        float fieldOfView = framing != null ? framing.FieldOfView : CloseUpFraming.DefaultFieldOfView;

        CloseUpShot shot = CloseUpPlacement.Choose(lookAt, tool.transform.forward, localPlayer.transform.position,
            distance, height, IsLineOfSightBlocked);

        CloseUpLookAt = lookAt;
        Transform cameraTransform = closeUpCamera.transform;
        cameraTransform.position = shot.Position;
        cameraTransform.rotation = shot.TopDown
            ? Quaternion.LookRotation(Vector3.down, localPlayer.transform.forward)
            : Quaternion.LookRotation(lookAt - shot.Position, Vector3.up);
        LensSettings lens = closeUpCamera.Lens;
        lens.FieldOfView = fieldOfView;
        closeUpCamera.Lens = lens;

        return machine.EnterCloseUp(tool);
    }

    // Stops short of the look-at point so the tool and the counter it sits on don't block their own shot.
    private bool IsLineOfSightBlocked(Vector3 from, Vector3 to)
    {
        Vector3 toTarget = to - from;
        float length = toTarget.magnitude - LineOfSightToolMargin;
        if (length <= 0f)
            return false;
        return Physics.Raycast(from, toTarget.normalized, length, closeUpBlockers, QueryTriggerInteraction.Ignore);
    }

    public void ExitCloseUp()
    {
        machine.ExitCloseUp();
    }

    private void SubscribePlacement()
    {
        IBuildingPlacementManager placement = GridBuildingSystem.Instance.BuildingPlacementManager;
        if (placement == null || placement == subscribedPlacement)
            return;
        subscribedPlacement = placement;
        placement.OnBuildingStart += Placement_OnBuildingStart;
        placement.OnBuildingEnd += Placement_OnBuildingEnd;
    }

    private void Placement_OnBuildingStart(object sender, EventArgs e) => machine.BuildStarted();
    private void Placement_OnBuildingEnd(object sender, EventArgs e) => machine.BuildEnded();

    private void GameInput_OnLookDelta(object sender, Vector2 delta)
    {
        if (machine.Mode != CameraMode.Walk || walkOrbit == null)
            return;
        walkOrbit.HorizontalAxis.Value += delta.x * lookDegreesPerPixel;
    }

    private void Machine_Changed(CameraMode mode)
    {
        ApplyPriorities();
        OnModeChanged?.Invoke(mode);
    }

    private void ApplyPriorities()
    {
        SetPriority(walkCamera, machine.Mode == CameraMode.Walk);
        SetPriority(closeUpCamera, machine.Mode == CameraMode.CloseUp);
        SetPriority(buildCamera, machine.Mode == CameraMode.Build);
    }

    private static void SetPriority(CinemachineCamera cam, bool live)
    {
        if (cam == null)
            return;
        cam.Priority.Enabled = true;
        cam.Priority.Value = live ? LivePriority : IdlePriority;
    }
}
