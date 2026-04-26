using Unity.Cinemachine;
using System;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private float _initCameraAngle;
    [SerializeField] private Camera _camera = null;
    [SerializeField] private CinemachineCamera _cinemachineCamera = null;
    [SerializeField] private float panSpeed = 0.1f;
    [SerializeField] private float minimumDistance = 0.2f;
    [SerializeField, Range(0,1)] private float directionThreshold = 0.3f;
    private Vector2 lastTouchPosition;
    private bool isRotating = false;

    [SerializeField] private float zoomSpeed = 1f;
    [SerializeField] private Vector2 zoomBounds = new(5f, 20f);
    private Vector2 panBounds = Vector2.zero;
    private Quaternion targetQuaternion;
    private float _lastPinchDistance;
    private void Awake()
    { 
        lastTouchPosition = Vector2.zero;

        panBounds = new Vector2Int(GameDefine.GridSize, GameDefine.GridSize);
    }

    private void Start()
    {
        //GameManager.Instance.OnPlayerSpawned += Instance_OnPlayerSpawned;
        GameInput.Instance.OnScrollPerformed += HandleZoom;
        GameInput.Instance.OnMousePanPerformed += OnPanMovedDesktop;
        GridBuildingSystem.Instance.OnObjectSpawned += Instance_OnObjectSpawned;


        UIHUDManager.Instance.OnRotateClicked += OnRotateClicked;
        
        ClampCameraPosition();

    }

    private void Instance_OnObjectSpawned()
    {
        GridBuildingSystem.Instance.BuildingPlacementManager.OnBuildingStart += OnBuildingStart;
        GridBuildingSystem.Instance.BuildingPlacementManager.OnBuildingEnd += OnBuildingEnd;
    }

    private void OnRotateClicked()
    {
        isRotating = true;

        targetQuaternion = Quaternion.Euler(_cinemachineCamera.transform.localEulerAngles.x, 
            _cinemachineCamera.transform.localEulerAngles.y + 90,
            _cinemachineCamera.transform.localEulerAngles.z);
    }

    private void Instance_OnPlayerSpawned(object sender, EventArgs e)
    {
        //_cinemachineCamera.Follow = PlayerStateMachine.Instance.transform;
    }

    private void Update()
    {
        if (isRotating)
        {
            _cinemachineCamera.transform.rotation = Quaternion.Lerp(_cinemachineCamera.transform.rotation,
                                                targetQuaternion, Time.deltaTime * 15f);
            if (_cinemachineCamera.transform.rotation == targetQuaternion)
                isRotating = false;
        }
    }
    private void ClampCameraPosition()
    {
        _cinemachineCamera.transform.position = new Vector3(Mathf.Clamp(_cinemachineCamera.transform.position.x, 3, panBounds.x),
            _cinemachineCamera.transform.position.y, 
            Mathf.Clamp(_cinemachineCamera.transform.position.z, -3, panBounds.y));
    }

    private void OnBuildingEnd(object sender, EventArgs e)
    {
        isRotating = true;
        targetQuaternion = Quaternion.Euler(_initCameraAngle, 0, 0);
    }
    private void OnBuildingStart(object sender, EventArgs e)
    {
        isRotating = true;
        targetQuaternion = Quaternion.Euler(90, 0, 0);
    }
    #region Pan
    private void OnPanMovedDesktop(object sender, Vector2 e)
    {
        if (GameInput.Instance.IsTouchOverBuildingGhost)
            return;
        panBounds = GridBuildingSystem.Instance.GridManager.GetGridSize()* 
            GridBuildingSystem.Instance.GridManager.GetCellSize();
        if (panBounds == Vector2Int.zero)
        {
            panBounds = new Vector2(GameDefine.GridSize, GameDefine.GridSize);
        }

        if (lastTouchPosition == Vector2.zero)
        {
            lastTouchPosition = e;
        }

        var newTouchPos = e;

        if (Vector2.Distance(newTouchPos, lastTouchPosition) < minimumDistance)
            return;

        var newTouchWorldPos = _camera.ScreenToWorldPoint(
            new Vector3(newTouchPos.x, newTouchPos.y, Camera.main.nearClipPlane));
        var lastTouchWorldPos = _camera.ScreenToWorldPoint(new Vector3(lastTouchPosition.x, lastTouchPosition.y, Camera.main.nearClipPlane));
        Vector3 delta = newTouchWorldPos - lastTouchWorldPos;
        delta.y = 0;
        _cinemachineCamera.transform.position -= new Vector3(delta.normalized.x, 0, delta.normalized.z) * panSpeed;
        if (panBounds == Vector2.zero)
        {
            return;
        }

        ClampCameraPosition();
        lastTouchPosition = newTouchPos;
    }
    #endregion

    #region Zoom
    private void HandleZoom(object sender, float scrollValueY)
    {
        _cinemachineCamera.Lens.FieldOfView = Mathf.Clamp(
            _cinemachineCamera.Lens.FieldOfView - scrollValueY * zoomSpeed,
            zoomBounds.x,
            zoomBounds.y);
    }
    #endregion
}
