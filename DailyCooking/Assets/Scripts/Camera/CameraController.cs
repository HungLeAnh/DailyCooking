using Cinemachine;
using System;
using System.Buffers;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using EnhancedTouch = UnityEngine.InputSystem.EnhancedTouch;

public class CameraController : MonoBehaviour
{
    [SerializeField] private CinemachineVirtualCamera _cam = null;
    [SerializeField] private float panSpeed = 0.1f;
    [SerializeField] private float minimumDistance = 0.2f;
    [SerializeField, Range(0,1)] private float directionThreshold = 0.3f;
    private Vector2 lastTouchPosition;
    private bool isPanning = false;


    [SerializeField] private float zoomSpeed = 1f;
    [SerializeField] Vector2 zoomBounds = new(5f, 20f);

    private float _lastPinchDistance;
    private void Awake()
    { 
        lastTouchPosition = Vector2.zero;
        EnhancedTouchSupport.Enable();
    }

    private void Start()
    {
        GameInput.Instance.OnFingerDown += OnPanStarted;
        GameInput.Instance.OnFingerUp += OnPanCanceled;
        GameInput.Instance.OnDragPerformed += OnPanMoved;
        GameInput.Instance.OnPintchPerformed += HandleZoom;

    }

    private void OnPanMoved(object sender, Finger e)
    {
        if (lastTouchPosition == Vector2.zero)
        {
            lastTouchPosition = e.currentTouch.screenPosition;
        }

        var newTouchPos = e.currentTouch.screenPosition;

        if (Vector2.Distance(newTouchPos, lastTouchPosition) < minimumDistance)
            return;

        var newTouchWorldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(newTouchPos.x, newTouchPos.y, Camera.main.nearClipPlane));
        var lastTouchWorldPos = Camera.main.ScreenToWorldPoint(new Vector3(lastTouchPosition.x, lastTouchPosition.y, Camera.main.nearClipPlane));
        Vector3 delta = newTouchWorldPos - lastTouchWorldPos;
        delta.y = 0;
        _cam.transform.position -= new Vector3(delta.normalized.x, 0, delta.normalized.z) * panSpeed;

        lastTouchPosition = newTouchPos;


    }
    private void OnPanStarted(object sender, Finger e)
    {
        isPanning = true;
        lastTouchPosition = e.screenPosition;
        //Debug.LogError(lastTouchPosition);
    }

    private void OnPanCanceled(object sender, Finger e)
    {
        isPanning = false;
        lastTouchPosition = Vector2.zero;
    }

    #region Zoom
    private void HandleZoom(object sender, EventArgs e)
    {
        HandleZoomMobile();

        //if (EnhancedTouch.Touch.activeTouches.Count == 2)
        //{
        //    HandleZoomMobile();
        //}
        //else
        //{
        //    HandleZoomDesktop();
        //}

    }

    private void HandleZoomDesktop()
    {
        float scrollValue = Mouse.current.scroll.ReadValue().magnitude;
        _cam.m_Lens.FieldOfView = Mathf.Clamp(
            _cam.m_Lens.FieldOfView - scrollValue * zoomSpeed,
            zoomBounds.x,
            zoomBounds.y);
    }

    private void HandleZoomMobile()
    {
        EnhancedTouch.Touch firstTouch = EnhancedTouch.Touch.activeTouches[0];
        EnhancedTouch.Touch secondTouch = EnhancedTouch.Touch.activeTouches[1];
        var touch0Current = firstTouch.screenPosition;
        var touch1Current = secondTouch.screenPosition;

        var touch0Previous = firstTouch.screenPosition - firstTouch.delta;
        var touch1Previous = secondTouch.screenPosition - secondTouch.delta;

        float currentDistance = Vector2.Distance(touch0Current, touch1Current);
        float previousDistance = Vector2.Distance(touch0Previous, touch1Previous);
        float difference = currentDistance - previousDistance;
        _cam.m_Lens.FieldOfView = Mathf.Clamp(
            _cam.m_Lens.FieldOfView - difference * zoomSpeed,
            zoomBounds.x,
            zoomBounds.y);
    }

    #endregion
}
