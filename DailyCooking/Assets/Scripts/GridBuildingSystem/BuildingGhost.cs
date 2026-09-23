using System;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
public class BuildingGhost : NetworkSimpleSingleton<BuildingGhost> 
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
    private bool stopMoving = false;
    private Quaternion targetQuaternion;
    private Vector3 targetPosition;
    private Vector3 pendingSpawnPosition;

    public bool IsDragging { get => isDragging; set => isDragging = value; }
    public bool StopMoving { get => stopMoving; set => stopMoving = value; }
    public Button ConfirmButton => confirmButton;
    private void Start() 
    {
        RefreshVisual(-Vector3.one);
        GameInput.Instance.OnMouseClickPerformed += OnPanStarted;
        GameInput.Instance.OnMouseClickCanceled += OnPanCanceled;
        if(GridBuildingSystem.Instance.IsInitialized)
        {
            Instance_OnObjectSpawned();
        }
        else
        {
            GridBuildingSystem.Instance.OnObjectSpawned += Instance_OnObjectSpawned;

        }

    }

    private void OnDestroy()
    {
        if (GameInput.Instance != null)
        {
            GameInput.Instance.OnMouseClickPerformed -= OnPanStarted;
            GameInput.Instance.OnMouseClickCanceled -= OnPanCanceled;
        }
        if (GridBuildingSystem.Instance != null)
        {
            GridBuildingSystem.Instance.OnObjectSpawned -= Instance_OnObjectSpawned;
            if (GridBuildingSystem.Instance.BuildingPlacementManager != null)
            {
                GridBuildingSystem.Instance.BuildingPlacementManager.OnSelectedChanged -= Instance_OnSelectedChanged;
            }
        }
        DestroyLocalPreview();
    }

    private void Instance_OnObjectSpawned()
    {
        GridBuildingSystem.Instance.BuildingPlacementManager.OnSelectedChanged += Instance_OnSelectedChanged;
    }

    private void Instance_OnSelectedChanged(object sender, GridBuildingSystem.OnSelectedChangedArgs args) 
    {
        Debug.Log("Selected Changed: " + args.placedObjectTypeSO);
        this.placedObjectTypeSO = args.placedObjectTypeSO;        
        targetPosition = args.position;
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
        if (placedObjectTypeSO == null || GameInput.Instance.IsMouseOverUI())
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
    }

    private void Update()
    {        
        HandleRotate();
        HandleDraging();
    }

    private void HandleRotate()
    {
        if (isRotating && visual != null)
        {
            visual.rotation = Quaternion.Lerp(visual.localRotation,targetQuaternion, Time.deltaTime * 20f);
            if (Quaternion.Angle(visual.localRotation,targetQuaternion)< 0.5)
                isRotating = false;
        }
    }

    private void HandleDraging()
    {
        if (isDragging == false || stopMoving || visual == null)
            return;
        targetPosition = GridBuildingSystem.Instance.BuildingPlacementManager.GetMouseWorldSnappedPosition();
        //Debug.Log("Dragging Building Ghost target pos: "+ targetPosition);
        if (targetPosition == -Vector3.one)
            return;

        targetPosition.y = 1f;
        ApplyToolSlotHeight(ref targetPosition);
        visualContainer.position = Vector3.Lerp(visualContainer.position, targetPosition, Time.deltaTime * 20f);
        visual.position = Vector3.Lerp(visual.position, targetPosition, Time.deltaTime * 20f);
        visual.rotation = Quaternion.Lerp(visual.localRotation, targetQuaternion, Time.deltaTime * 20f);

        OnBUildingDrag?.Invoke(targetPosition);
    }

    private void RefreshVisual(Vector3 position) {
        DestroyLocalPreview();

        if (placedObjectTypeSO != null)
        {
            if (placedObjectTypeSO.prefab == null)
            {
                Debug.LogError($"BuildingGhost: Missing prefab for PlacedObjectTypeSO '{placedObjectTypeSO.name}' Guid={placedObjectTypeSO.Guid}", placedObjectTypeSO);
                ShowCanvas(false);
                return;
            }
            // Guard sentinel - resolve to mouse snapped pos (option a)
            if (position == -Vector3.one)
            {
                Vector3 snapped = GridBuildingSystem.Instance.BuildingPlacementManager.GetMouseWorldSnappedPosition();
                if (snapped == -Vector3.one)
                {
                    var gm = GridBuildingSystem.Instance.GridManager;
                    if (gm != null)
                    {
                        snapped = gm.GetWorldPosition(0, 0);
                        snapped.y = 0f;
                    }
                    else
                    {
                        snapped = Vector3.zero;
                    }
                }
                position = snapped;
            }
            position.y = 1f;
            ApplyToolSlotHeight(ref position);
            pendingSpawnPosition = position;

            visual = CreateLocalPreview(placedObjectTypeSO, position);
            SetLayerRecursive(visual.gameObject, LayerMask.NameToLayer("BuildingGhost"));
            ShowCanvas(true);

            visualContainer.position = pendingSpawnPosition;
            visual.position = pendingSpawnPosition;
        }
        else
        {
            ShowCanvas(false);
        }
    }

    // The preview exists only on this client. It is a copy of the prefab created under an
    // inactive holder, so its scripts, NavMeshObstacles and network components are switched off
    // before they ever run; colliders stay for drag raycasts. The holder mirrors the container
    // real placed objects live in, so local positions match.
    private Transform CreateLocalPreview(PlacedObjectTypeSO previewTypeSO, Vector3 position)
    {
        GameObject source = previewTypeSO.visual != null ? previewTypeSO.visual : previewTypeSO.prefab;
        Transform container = GridBuildingSystem.Instance.Container;
        var holder = new GameObject("BuildingGhostPreview");
        holder.SetActive(false);
        if (container != null)
        {
            holder.transform.SetPositionAndRotation(container.position, container.rotation);
            holder.transform.localScale = container.lossyScale;
        }

        GameObject preview = Instantiate(source, position, Quaternion.identity, holder.transform);
        foreach (MonoBehaviour behaviour in preview.GetComponentsInChildren<MonoBehaviour>(true))
            behaviour.enabled = false;
        foreach (UnityEngine.AI.NavMeshObstacle obstacle in preview.GetComponentsInChildren<UnityEngine.AI.NavMeshObstacle>(true))
            obstacle.enabled = false;

        holder.SetActive(true);
        return preview.transform;
    }

    private void DestroyLocalPreview()
    {
        if (visual == null)
            return;
        Destroy(visual.parent != null ? visual.parent.gameObject : visual.gameObject);
        visual = null;
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
//        KitchenGameManager.Instance.DestroyPlacedObject(visual.GetComponent<NetworkObject>());

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
        if(visual == null)
            return;
        visual.localPosition = targetPosition + GridBuildingSystem.Instance.BuildingPlacementManager
            .GetPlacedObjectRotationOffset(placedObjectTypeSO.itemType.TabType);
    }

    public void SnapTo(Vector3 targetPoint)
    {
        targetPoint.y = 1f;
        ApplyToolSlotHeight(ref targetPoint);
        visualContainer.position = targetPoint;
        visual.localRotation =  targetQuaternion;
    }

    private void ApplyToolSlotHeight(ref Vector3 pos)
    {
        if (placedObjectTypeSO == null || !placedObjectTypeSO.isTool) return;
        GridBuildingSystem gbs = GridBuildingSystem.Instance;
        if (gbs == null || gbs.GridManager == null) return;
        gbs.GridManager.GetXZ(new Vector3(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y), Mathf.RoundToInt(pos.z)), out int x, out int z);
        Vector2Int origin = gbs.GridManager.ValidateGridPosition(new Vector2Int(x, z));
        if (ToolSlotResolver.TryFindUnderlyingCounter(gbs.GridManager.Grid, placedObjectTypeSO, origin, out PlacedObjectView counterView) &&
            ToolSlotResolver.TryGetToolSlotPosition(counterView, origin, out Vector3 slotPos))
        {
            pos = slotPos;
        }
    }
}

