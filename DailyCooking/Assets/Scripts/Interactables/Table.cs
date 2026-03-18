using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Table : NetworkBehaviour,IKitchenObjectParent, IDestroyable, IPlaceable, IModuleItem
{
    [SerializeField] private List<Transform> seats = new List<Transform>();
    [SerializeField] private List<Transform> kitchenObjectFollowPoints = new List<Transform>();
    [SerializeField] private GameObject[] visualGameObjectArray;
    
    private bool[] isSeatOccupied;
    private KitchenObject[] kitchenObjects;
    private NetworkVariable<bool> isPlaced = new NetworkVariable<bool>(false);
    private Action onDestroySelf;
    public Action OnDestroySelf { get => onDestroySelf;  set => onDestroySelf += value; }
    public NetworkVariable<bool> IsPlaced { get => isPlaced; set => isPlaced = value; }
    public override void OnNetworkSpawn()
    {
        
    }
    private void Start()
    {
        isSeatOccupied = new bool[seats.Count];
        kitchenObjects = new KitchenObject[seats.Count];
        //KitchenGameManager.Instance.OnStateChanged += KitchenGameManager_OnStateChanged;
    }
    private void KitchenGameManager_OnStateChanged(object sender, EventArgs e)
    {
        if (KitchenGameManager.Instance.IsEditing())
        {
            ResetTable();
        }
    }

    private void OnDestroy()
    {
        if(TableManager.Instance != null)
            TableManager.Instance.UnregisterTable(this);
        //if(KitchenGameManager.Instance != null)
        //    KitchenGameManager.Instance.OnStateChanged -= KitchenGameManager_OnStateChanged;
    }

    public int GetAvailableSeat()
    {
        for (int i = 0; i < seats.Count; i++)
        {
            if (!isSeatOccupied[i] && kitchenObjects[i] == null)
            {
                return i;
            }
        }
        return -1; // No available seat
    }

    public bool OccupySeat(int seatIndex)
    {
        if (seatIndex < 0 || seatIndex >= seats.Count || isSeatOccupied[seatIndex])
        {
            return false; // Invalid seat index or seat is already occupied
        }

        isSeatOccupied[seatIndex] = true;
        return true;
    }
    [Rpc(SendTo.Server)]
    public void OccupySeatServerRpc(int seatIndex)
    {
        OccupySeatClientRpc(seatIndex);
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void OccupySeatClientRpc(int seatIndex)
    {
        OccupySeat(seatIndex);
    }
    public void ResetTable()
    {
        for (int i = 0; i < isSeatOccupied.Length; i++)
        {
            isSeatOccupied[i] = false;
        }

        for(int i = 0; i < kitchenObjects.Length; i++)
        {
            if(kitchenObjects[i] != null)
            {
                kitchenObjects[i].DestroySelf(i);
                kitchenObjects[i] = null;
            }
        }
    }
    public void ResetSeat(int index)
    {
        if(index < 0 || index >= isSeatOccupied.Length)
        {
            return;
        }
        isSeatOccupied[index] = false;
    }
    [Rpc(SendTo.Server)]
    public void ResetSeatServerRpc(int index)
    {
        ResetSeatClientRpc(index);
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void ResetSeatClientRpc(int index)
    {
        ResetSeat(index);
    }
    public Transform GetSeatTransform(int seatIndex)
    {
        if (seatIndex >= 0 && seatIndex < seats.Count)
        {
            return seats[seatIndex];
        }
        return null;
    }

    public Transform GetKitchenObjectFollowTransform(int index = 0)
    {
        if (kitchenObjectFollowPoints != null && index >= 0 && index < kitchenObjectFollowPoints.Count)
        {
            return kitchenObjectFollowPoints[index];
        }
        return null;
    }

    public void SetKitchenObject(KitchenObject kitchenObject, int index = 0)
    {
        if (kitchenObjects != null && index >= 0 && index < kitchenObjectFollowPoints.Count)
        {
            kitchenObjects[index] = kitchenObject;
        }

    }

    public KitchenObject GetKitchenObject(int index = 0)
    {
        if (kitchenObjects != null && index >= 0 && index < kitchenObjects.Length)
        {
            return kitchenObjects[index];
        }
        return null;
    }

    public void ClearKitchenObject(int index = 0)
    {
        ClearKitchenObjectServerRpc(index);
    }
    [Rpc(SendTo.Server)]
    private void ClearKitchenObjectServerRpc(int index = 0)
    {
        ClearKitchenObjectClientRpc(index);
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void ClearKitchenObjectClientRpc(int index = 0)
    {
        if (kitchenObjects != null && index >= 0 &&
            index < kitchenObjects.Length &&
            index < seats.Count)
        {
            kitchenObjects[index] = null;
            isSeatOccupied[index] = false;

        }
    }

    public bool HasKitchenObject(int index = 0)
    {
        Debug.Log("Table kitchen object length: " + kitchenObjects.Length);
        if(kitchenObjects != null && 
            kitchenObjects.Length > 0 && 
            index < kitchenObjects.Length)
        {
            Debug.Log("Check kitchen object at index " + index + ": " + (kitchenObjects[index]));
            return kitchenObjects[index] != null;
        }
        else
        {
            return false;
        }
    }
    public void SetEatenViual(int index, int cash, int exp)
    {
        var tablewareObject = kitchenObjects[index] as TablewareKitchenObject;

        if(tablewareObject != null)
        {
            tablewareObject.SetEaten(cash, exp);
        }
    }
    [Rpc(SendTo.Server)]
    public void SetEatenVisualServerRpc(int index, int cash, int exp)
    {
        SetEatenVisualClientRpc(index, cash, exp);
    }
    [Rpc(SendTo.ClientsAndHost)]
    private void SetEatenVisualClientRpc(int index, int cash, int exp)
    {
        SetEatenViual(index, cash, exp);
    }
    public void DestroySelf()
    {
        OnDestroySelf?.Invoke();
        NetworkObject.Despawn();
        Destroy(this);
    }

    public bool CanRemove()
    {
        for(int i = 0; i < isSeatOccupied.Length; i++)
        {
            if (isSeatOccupied[i])
                return false;
        }
        for(int i = 0; i < kitchenObjects.Length; i++)
        {
            if (kitchenObjects[i] != null)
                return false;
        }
        return true;
    }

    public void RegisterItem()
    {
        TableManager.Instance.RegisterTable(this);
    }

    public NetworkObject GetNetworkObject()
    {
        return NetworkObject;
    }
}
