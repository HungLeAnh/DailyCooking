using System;
using UnityEngine;
using UnityEngine.UI;
public class BuildingGhost : SimpleSingleton<BuildingGhost> 
{
    public Action<Vector3> OnBUildingDrag;
    [SerializeField] private GameObject buildingCanvas;
    [SerializeField] private GameObject buildingGhostCanvas;
    [SerializeField] private LayerMask buildingGhostLayer;
    [SerializeField] private Camera _camera = null;
    [SerializeField] private Transform visualContainer;
    [SerializeField] private Button confirmButton;

    private Transform visual;
    private PlacedObjectTypeSO placedObjectTypeSO;

    private bool isDragging = false;
    private bool isRotating = false;
    private Quaternion targetQuaternion;

    public bool IsDragging { get => isDragging; set => isDragging = value; }
    public Button ConfirmButton => confirmButton;
    private void Start() 
    {
        RefreshVisual(-Vector3.one);
        GameInput.Instance.OnMouseClickPerformed += OnPanStarted;
        GameInput.Instance.OnMouseClickCanceled += OnPanCanceled;
        GridBuildingSystem.Instance.BuildingPlacementManager.OnSelectedChanged += Instance_OnSelectedChanged;
    }

    private void Instance_OnSelectedChanged(object sender, GridBuildingSystem.OnSelectedChangedArgs args) 
    {
        this.placedObjectTypeSO = args.placedObjectTypeSO;        
        RefreshVisual(args.position);

        if (this.placedObjectTypeSO == null)
        {
            return;
        }
        targetQuaternion = GridBuildingSystem.Instance.BuildingPlacementManager.GetPlacedObjectRotation();
        isRotating = true;
        OffsetRotation();

    }
    private void OnPanCanceled(object sender, EventArgs e)
    {
        isDragging = false;

    }

    private void OnPanStarted(object sender, Vector2 e)
    {
        if (placedObjectTypeSO == null)
        {
            return;
        }

        float interactDistance = 999f;

        Ray ray = _camera.ScreenPointToRay(e);
        if (Physics.Raycast(ray, out RaycastHit raycastHit, interactDistance, buildingGhostLayer))
        {
            //Debug.Log("Touch Position: " + raycastHit.point);
            isDragging = true;

        }
        else
        {
            isDragging = false;
        }

        if (Physics.Raycast(ray, out RaycastHit raycastHitDebug, interactDistance))
        { 
            Debug.Log("Hit object: " + raycastHitDebug.transform.gameObject);
        }
        else
        {
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
            visual.localRotation = Quaternion.Lerp(visual.localRotation,targetQuaternion, Time.deltaTime * 20f);
            if (Quaternion.Angle(visual.localRotation,targetQuaternion)< 0.5)
                isRotating = false;
        }
    }

    private void HandleDraging()
    {
        if (isDragging == false)
            return;
        Vector3 targetPosition = GridBuildingSystem.Instance.BuildingPlacementManager.GetMouseWorldSnappedPosition();
        //Debug.Log("Dragging Building Ghost target pos: "+ targetPosition);
        if (targetPosition == -Vector3.one)
            return;

        targetPosition.y = 1f;
        visualContainer.position = Vector3.Lerp(visualContainer.position, targetPosition, Time.deltaTime * 20f);
        visual.localRotation = Quaternion.Lerp(visual.localRotation, targetQuaternion, Time.deltaTime * 20f);

        OnBUildingDrag?.Invoke(targetPosition);
    }

    private void RefreshVisual(Vector3 targetPosition) {
        if (visual != null) {
            Destroy(visual.gameObject);
            visual = null;
        }

        if (placedObjectTypeSO != null)
        {
            PlacedObjectView placedObjectView = PlacedObjectFactory.Create(Vector3.zero, Vector2Int.zero, Dir.Down, placedObjectTypeSO);
            placedObjectView.GetComponent<IPlaceable>().IsPlaced = false;

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
        if (GridBuildingSystem.Instance.BuildingPlacementManager.TryPlaceBuildingObject(visualContainer.position))
        {
            UIPopupManager.Instance.ShowPopup(UIPopupType.UIInventoryPopup);
            ShowCanvas(false);
        }
        else
        {
            UIPopupManager.Instance.ShowPopup(UIPopupType.UIGameNotiPopup,
               new UIGameNotiPopup.Param
               {
                   Title = "warning",
                   Message = "Can't build here!"
               });
        }
    }
    public void OnClickCancel()
    {
        isDragging = false;
        isRotating = false;
        visual.gameObject.GetComponent<PlacedObjectView>().DestroySelf();

        UIPopupManager.Instance.ShowPopup(UIPopupType.UIInventoryPopup);
        ShowCanvas(false);
        GridBuildingSystem.Instance.BuildingPlacementManager.ReturnObjectToInventory();
        GridBuildingSystem.Instance.BuildingPlacementManager.SetPlacedObjectTypeSO(null,-Vector3.one);
    }
    public void OnClickRotate()
    {
        if (placedObjectTypeSO != null)
        {
            GridBuildingSystem.Instance.BuildingPlacementManager.RotateBuildingObject();
            targetQuaternion = GridBuildingSystem.Instance.BuildingPlacementManager.GetPlacedObjectRotation();
            isRotating = true;
            OffsetRotation();
        }
    }

    private void OffsetRotation()
    {
        visual.localPosition = GridBuildingSystem.Instance.BuildingPlacementManager
            .GetPlacedObjectRotationOffset(placedObjectTypeSO.itemType.TabType);
    }

    public void SnapTo(Vector3 targetPoint)
    {
        targetPoint.y = 1f;
        visualContainer.position = targetPoint;
        visual.localRotation =  targetQuaternion;

    }
}

