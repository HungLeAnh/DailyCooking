using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Netcode;

[Serializable]
public class ContainerData : GridObjectData
{
    private List<ContainerDataSerializable> containerDataSerializableList;
    public List<ContainerDataSerializable> ContainerDataSerializableList { get => containerDataSerializableList; set => containerDataSerializableList = value; }
    public ContainerData(List<ContainerDataSerializable> containerDataSerializableList, string placedObjectTypeSOGuid, Vector2Int origin, Dir dir, InventoryTabType type)
        : base(placedObjectTypeSOGuid, origin, dir, type)
    {
        this.containerDataSerializableList = containerDataSerializableList;
    }

}
[Serializable]
public struct ContainerDataSerializable : INetworkSerializable,IEquatable<ContainerDataSerializable>
{
    [System.NonSerialized]
    public FixedString64Bytes KitchenObjectSOGuid;
    public float FillAmount;
    // This property will be used by the Serializer
    public string KitchenObjectSOGuidString
    {
        get => KitchenObjectSOGuid.ToString();
        set => KitchenObjectSOGuid = value;
    }
    public ContainerDataSerializable(string kitchenObjectSOGuid, float fillAmount)
    {
        KitchenObjectSOGuid = kitchenObjectSOGuid;
        FillAmount = fillAmount;
    }

    public bool Equals(ContainerDataSerializable other)
    {
        return KitchenObjectSOGuid.Equals(other.KitchenObjectSOGuid) && FillAmount == other.FillAmount;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref KitchenObjectSOGuid);
        serializer.SerializeValue(ref FillAmount);
    }
}