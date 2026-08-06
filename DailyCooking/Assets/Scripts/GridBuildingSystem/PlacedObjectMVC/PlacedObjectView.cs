using DG.Tweening.Core.Easing;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;

[Serializable]
public class PlacedObjectView : NetworkBehaviour
{
    private NetworkVariable<FixedString64Bytes> placedObjectTypeSOGuid = new NetworkVariable<FixedString64Bytes>();
    private NetworkVariable<Vector2Int> origin = new NetworkVariable<Vector2Int>();
    private NetworkVariable<Dir> dir = new NetworkVariable<Dir>();
    private NetworkVariable<bool> isPreview = new NetworkVariable<bool>();
    private PlacedObjectTypeSO placedObjectTypeSO;
    public Vector2Int Origin => origin.Value;
    public Dir Dir => dir.Value;
    public PlacedObjectTypeSO PlacedObjectTypeSO => placedObjectTypeSO;
    public InventoryTabType InventoryTabType => placedObjectTypeSO.itemType.TabType;

    public NetworkVariable<bool> IsPreview { get => isPreview; set => isPreview = value; }

    public override void OnNetworkSpawn()
    {
        if (MultiplayerManager.Instance.IsHost || MultiplayerManager.Instance.IsServer)
        {
            OnSpawned();
        }
        else
        {
            GridBuildingSystem.Instance.OnObjectSpawned += GridBuildingSystem_OnObjectSpawned;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (GridBuildingSystem.Instance != null)
        {
            GridBuildingSystem.Instance.OnObjectSpawned -= GridBuildingSystem_OnObjectSpawned;
        }
        if (placedObjectTypeSOGuid != null)
        {
            placedObjectTypeSOGuid.OnValueChanged -= PlacedObjectTypeSOGuid_OnValueChanged;
        }
    }

    private void GridBuildingSystem_OnObjectSpawned()
    {
        OnSpawned();
    }

    private void PlacedObjectTypeSOGuid_OnValueChanged(FixedString64Bytes previousValue, FixedString64Bytes newValue)
    {
        //Debug.Log("PlacedObjectView: placedObjectTypeSOGuid changed: " + newValue);
        placedObjectTypeSO = GridBuildingSystem.Instance.GetPlacedObjectTypeSOByGuid(newValue.ToString());
    }

    private void OnSpawned()
    {
        placedObjectTypeSOGuid.OnValueChanged += PlacedObjectTypeSOGuid_OnValueChanged;
        this.placedObjectTypeSO = GridBuildingSystem.Instance.GetPlacedObjectTypeSOByGuid(placedObjectTypeSOGuid.Value.ToString());

        if (!isPreview.Value)
        {
            //Debug.Log("PlaceObjectType : " + PlacedObjectTypeSO);
            //Debug.Log("PlaceObjectTypeGuid : " + GetPlacedObjectTypeSOGuid());
            //Debug.Log("GridManager : " + GridBuildingSystem.Instance.GridManager);
            if(GridBuildingSystem.Instance.GridManager == null)
            {
                Debug.LogError("GridManager is null");
                return;
            }
            List<Vector2Int> gridPositionList = GetGridPositionList();
            foreach (var gridPosition in gridPositionList)
            {
                GridBuildingSystem.Instance.GridManager.Grid.AddGridObjectData(gridPosition.x, gridPosition.y,
                    new GridObject(GridBuildingSystem.Instance.GridManager.Grid, this, gridPosition.x, gridPosition.y));
            }

            this.GetComponent<IModuleItem>()?.RegisterItem();
        }
    }
    public void Intialize(string placedObjectTypeSOGuid, Vector2Int origin, Dir dir,bool isPreview)
    {
        this.isPreview.Value = isPreview;
        this.placedObjectTypeSOGuid.Value = placedObjectTypeSOGuid;
        this.origin.Value = origin;
        this.dir.Value = dir;
    }
    public void UpdateDirAndOrigin(Vector2Int origin, Dir dir)
    {
        this.origin.Value = origin;
        this.dir.Value = dir;
    }

    public List<Vector2Int> GetGridPositionList()
    {
        this.placedObjectTypeSO = GridBuildingSystem.Instance.GetPlacedObjectTypeSOByGuid(placedObjectTypeSOGuid.Value.ToString());
        return placedObjectTypeSO.GetGridPositionList(origin.Value, dir.Value);
    }

    public override string ToString()
    {
        return placedObjectTypeSO.nameString;
    }

    public string GetPlacedObjectTypeSOGuid()
    {
        return placedObjectTypeSOGuid.Value.ToString();
    }
}