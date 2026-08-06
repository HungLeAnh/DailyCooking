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
        if (KitchenGameManager.Instance != null)
        {
            KitchenGameManager.Instance.OnSpawnRequestCompleted -= OnSpawnRequestCompletedHandler;
        }
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
        visualContainer.position = Vector3.Lerp(visualContainer.position, targetPosition, Time.deltaTime * 20f);
        visual.position = Vector3.Lerp(visual.position, targetPosition, Time.deltaTime * 20f);
        visual.rotation = Quaternion.Lerp(visual.localRotation, targetQuaternion, Time.deltaTime * 20f);

        OnBUildingDrag?.Invoke(targetPosition);
    }

    private void RefreshVisual(Vector3 position) {
        if (visual != null) {
            KitchenGameManager.Instance.DestroyPlacedObject(visual.GetComponent<NetworkObject>());
            visual = null;
        }

        if (placedObjectTypeSO != null)
        {
            pendingSpawnPosition = position;
            KitchenGameManager.Instance.OnSpawnRequestCompleted -= OnSpawnRequestCompletedHandler;
            KitchenGameManager.Instance.OnSpawnRequestCompleted += OnSpawnRequestCompletedHandler;

            position.y = 1f;
            Debug.Log("Creating Placed Object at: " + position);
            PlacedObjectFactory.Create(position, Vector2Int.zero, Dir.Down,
                placedObjectTypeSO, NetworkManager.Singleton.LocalClientId,true);
            
        }
        else
        {
            ShowCanvas(false);
        }
    }

    private void OnSpawnRequestCompletedHandler(NetworkObject spawnedObject)
    {
        KitchenGameManager.Instance.OnSpawnRequestCompleted -= OnSpawnRequestCompletedHandler;
        //Debug.Log("Spawned Object: " + spawnedObject.name);
        PlacedObjectView placedObjectView = spawnedObject.GetComponent<PlacedObjectView>();

        visual = placedObjectView.transform;
        SetLayerRecursive(visual.gameObject, LayerMask.NameToLayer("BuildingGhost"));
        ShowCanvas(true);

        visualContainer.position = pendingSpawnPosition;
        visual.position = new Vector3(pendingSpawnPosition.x, 4f, pendingSpawnPosition.z);
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
        visualContainer.position = targetPoint;
        visual.localRotation =  targetQuaternion;
    }
}

