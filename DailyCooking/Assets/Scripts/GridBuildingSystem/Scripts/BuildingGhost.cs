using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
public class BuildingGhost : MonoBehaviour 
{
    [SerializeField] private GameObject buildingCanvas;
    [SerializeField] private GameObject buildingGhostCanvas;
    [SerializeField] private LayerMask buildingGhostLayer;
    [SerializeField] private Camera _camera = null;
    [SerializeField] private Transform visualContainer;


    private Transform visual;
    private PlacedObjectTypeSO placedObjectTypeSO;

    private bool isDragging = false;
    private bool isRotating = false;
    private Quaternion targetQuaternion;

    private void Start() 
    {
        RefreshVisual(-Vector3.one);
        GameInput.Instance.OnFingerDown += OnPanStarted;
        GameInput.Instance.OnFingerUp += OnPanCanceled;
        GridBuildingSystem.Instance.OnSelectedChanged += Instance_OnSelectedChanged;
    }

    private void Instance_OnSelectedChanged(object sender, GridBuildingSystem.OnSelectedChangedArgs args) 
    {
        this.placedObjectTypeSO = args.placedObjectTypeSO;
        RefreshVisual(args.position);
    }
    private void OnPanCanceled(object sender, Finger e)
    {
        isDragging = false;

    }

    private void OnPanStarted(object sender, Finger e)
    {
        if (placedObjectTypeSO == null)
        {
            return;
        }

        float interactDistance = 999f;

        Ray ray = _camera.ScreenPointToRay(e.screenPosition);
        if (Physics.Raycast(ray, out RaycastHit raycastHit, interactDistance, buildingGhostLayer))
        {
            //Debug.Log("Touch Position: " + pos);
            isDragging = true;

        }
        else
        {
            isDragging = false;
        }
    }

    private void Update()
    {        
        HandleRotate();
        HandleDraging();
    }

    private void HandleRotate()
    {
        if (isRotating)
        {
            visualContainer.rotation = Quaternion.Lerp(visualContainer.rotation,targetQuaternion, Time.deltaTime * 20f);
            if (visualContainer.rotation == targetQuaternion)
                isRotating = false;
        }
    }

    private void HandleDraging()
    {
        if (isDragging == false)
            return;

        Vector3 targetPosition = GridBuildingSystem.Instance.GetMouseWorldSnappedPosition();
        if (targetPosition == -Vector3.one)
            return;

        targetPosition.y = 1f;
        visualContainer.position = Vector3.Lerp(visualContainer.position, targetPosition, Time.deltaTime * 20f);
        visualContainer.rotation = Quaternion.Lerp(visualContainer.rotation, targetQuaternion, Time.deltaTime * 20f);
    }

    private void RefreshVisual(Vector3 targetPosition) {
        if (visual != null) {
            Destroy(visual.gameObject);
            visual = null;
        }

        if (placedObjectTypeSO != null)
        {
            PlacedObjectView placedObjectView = PlacedObjectFactory.Create(Vector3.zero, Vector2Int.zero, Dir.Down, placedObjectTypeSO);
            visual = placedObjectView.transform;
            visual.parent = visualContainer;
            visual.localPosition = Vector3.zero;
            visual.localEulerAngles = Vector3.zero;
            SetLayerRecursive(visual.gameObject, LayerMask.NameToLayer("BuildingGhost"));
            ShowCanvas(true);
            if (targetPosition != -Vector3.one)
            {
                visualContainer.position = targetPosition;
            }
            else
            {
                visualContainer.position = Vector3.zero;
            }
        }
        else
        {
            ShowCanvas(false);
        }
    }



    private void SetLayerRecursive(GameObject targetGameObject, LayerMask layer) 
    {
        if (targetGameObject == null) return;

        targetGameObject.layer = layer;
        foreach (Transform child in targetGameObject.transform) 
        {
            if (child == targetGameObject) continue;
            SetLayerRecursive(child.gameObject, layer);
        }
    }    
    private void ShowCanvas(bool isShow)
    {
        buildingCanvas.SetActive(isShow);
        buildingGhostCanvas.SetActive(isShow);
    }
    public void OnClickConfirm()
    {
        GridBuildingSystem.Instance.PlaceBuildingObject(visualContainer.position);
        UIPopupManager.Instance.ShowPopup(UIPopupType.UIInventoryPopup.ToString());
        ShowCanvas(false);
    }
    public void OnClickCancel()
    {
        visual.gameObject.GetComponent<PlacedObjectView>().DestroySelf();

        UIPopupManager.Instance.ShowPopup(UIPopupType.UIInventoryPopup.ToString());
        ShowCanvas(false);
        GridBuildingSystem.Instance.SetPlacedObjectTypeSO(null,-Vector3.one);

    }
    public void OnClickRotate()
    {
        if (placedObjectTypeSO != null)
        {
            GridBuildingSystem.Instance.RotateBuildingObject();
            targetQuaternion = GridBuildingSystem.Instance.GetPlacedObjectRotation();
            isRotating = true;
        }
    }
}

