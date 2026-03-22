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
    private PlacedObjectTypeSO placedObjectTypeSO;
    public Vector2Int Origin => origin.Value;
    public Dir Dir => dir.Value;
    public PlacedObjectTypeSO PlacedObjectTypeSO => placedObjectTypeSO;
    public InventoryTabType InventoryTabType => placedObjectTypeSO.itemType.TabType;
    public override void OnNetworkSpawn()
    {
        placedObjectTypeSOGuid.OnValueChanged += (FixedString64Bytes previousValue, FixedString64Bytes newValue) =>
        {
            Debug.Log("PlacedObjectView: placedObjectTypeSOGuid changed: " + newValue);
            placedObjectTypeSO = GridBuildingSystem.Instance.GetPlacedObjectTypeSOByGuid(newValue.ToString());
        };
        this.placedObjectTypeSO = GridBuildingSystem.Instance.GetPlacedObjectTypeSOByGuid(placedObjectTypeSOGuid.Value.ToString());

    }
    public void Intialize(string placedObjectTypeSOGuid, Vector2Int origin, Dir dir)
    {
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