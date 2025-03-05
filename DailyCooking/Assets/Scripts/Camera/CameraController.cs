using Cinemachine;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraController : MonoBehaviour
{
    private PlayerAction playerAction;
    private Vector2 panStartPosition;
    private Vector2 panEndPosition;
    private bool isPanning = false;

    [SerializeField] private CinemachineBrain cinemachineBrain = null;
    [SerializeField] private float panSpeed = 0.1f;
    [SerializeField] private float zoomSpeed = 1f;
    [SerializeField] private float minZoom = 5f;
    [SerializeField] private float maxZoom = 50f;
    [SerializeField] private float minimumDistance = 0.2f;
    [SerializeField, Range(0,1)] private float directionThreshold = 0.3f;

    private void Awake()
    {
        playerAction = GameInput.Instance.PlayerAction;
        panStartPosition = Vector2.zero;
        panEndPosition = Vector2.zero;
    }

    private void Start()
    {
        GameInput.Instance.OnFingerDown += OnPanStarted;
        GameInput.Instance.OnFingerUp += OnPanCanceled;
        GameInput.Instance.OnFingerMoved += OnPanMoved;

    }

    private void OnPanMoved(object sender, GameInput.OnFingerEventArgs e)
    {
        panEndPosition = e.Position;
        if(e.IsPaused) 
            panStartPosition = e.Position;
        if (isPanning)
        {
            DetectSwipe();
        }
    }
    private void OnPanStarted(object sender, GameInput.OnFingerEventArgs e)
    {
        isPanning = true;
        panStartPosition = e.Position;
        //Debug.LogError(panStartPosition);
    }

    private void OnPanCanceled(object sender , GameInput.OnFingerEventArgs e)
    {
        isPanning = false;
        panEndPosition = Vector2.zero;
    }

    private void DetectSwipe()
    {
        if(Vector2.Distance(panStartPosition, panEndPosition) >= minimumDistance)
        { 
            Vector3 dir = panEndPosition - panStartPosition;
            Vector2 dir2D = new Vector2(dir.x, dir.y).normalized;
            SwipeDirection(-dir2D);
        }
    }

    private void SwipeDirection(Vector2 direction)
    {
        if(direction.magnitude > directionThreshold)
        {
            cinemachineBrain.ActiveVirtualCamera
                .VirtualCameraGameObject
                .transform.position += new Vector3(direction.x, 0, direction.y) * panSpeed;
        }
    }

    private void OnZoomPerformed(InputAction.CallbackContext context)
    {
        Vector2 scrollDelta = context.ReadValue<Vector2>();
        float zoomDelta = scrollDelta.y * zoomSpeed;
        cinemachineBrain.ActiveVirtualCamera.VirtualCameraGameObject
            .GetComponent<CinemachineVirtualCamera>().m_Lens.FieldOfView = Mathf.Clamp(Camera.main.fieldOfView - zoomDelta, minZoom, maxZoom);
    }
}
