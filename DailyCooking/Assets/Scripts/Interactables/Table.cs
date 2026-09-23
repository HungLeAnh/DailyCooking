using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Table : NetworkBehaviour,IKitchenObjectParent, IDestroyable, IPlaceable, IModuleItem
{
    [SerializeField] private List<Transform> seats = new List<Transform>();
    [SerializeField] private List<Transform> kitchenObjectFollowPoints = new List<Transform>();
    [SerializeField] private GameObject[] visualGameObjectArray;

    // Server-written, so every peer and late joiners agree on which seats are taken.
    private NetworkList<bool> isSeatOccupied;
    // Filled on every peer from the kitchen objects' replicated parent.
    private KitchenObject[] kitchenObjects;
    public event Action OnDestroySelf;

    private void Awake()
    {
        isSeatOccupied = new NetworkList<bool>();
        kitchenObjects = new KitchenObject[seats.Count];
    }
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsServer && isSeatOccupied.Count != seats.Count)
        {
            isSeatOccupied.Clear();
            for (int i = 0; i < seats.Count; i++)
                isSeatOccupied.Add(false);
        }
    }
    private void KitchenGameManager_OnStateChanged(object sender, EventArgs e)
    {
        if (KitchenGameManager.Instance.IsEditing())
        {
            ResetTable();
        }
    }

    public override void OnDestroy()
    {
        if(TableManager.Instance != null)
            TableManager.Instance.UnregisterTable(this);
        //if(KitchenGameManager.Instance != null)
        //    KitchenGameManager.Instance.OnStateChanged -= KitchenGameManager_OnStateChanged;
        base.OnDestroy();
    }

    private bool IsSeatOccupied(int seatIndex)
    {
        return seatIndex >= 0 && seatIndex < isSeatOccupied.Count && isSeatOccupied[seatIndex];
    }

    public int GetAvailableSeat()
    {
        for (int i = 0; i < seats.Count; i++)
        {
            if (!IsSeatOccupied(i) && kitchenObjects[i] == null)
            {
                return i;
            }
        }
        return -1; // No available seat
    }

    // Server only.
    public bool OccupySeat(int seatIndex)
    {
        if (!IsServer || seatIndex < 0 || seatIndex >= isSeatOccupied.Count || isSeatOccupied[seatIndex])
        {
            return false; // Invalid seat index or seat is already occupied
        }

        isSeatOccupied[seatIndex] = true;
        return true;
    }
    // Server only.
    public void ResetTable()
    {
        if (!IsServer) return;
        for (int i = 0; i < isSeatOccupied.Count; i++)
        {
            isSeatOccupied[i] = false;
        }

        for(int i = 0; i < kitchenObjects.Length; i++)
        {
            if(kitchenObjects[i] != null)
            {
                kitchenObjects[i].DestroySelf(i);
            }
        }
    }
    // Server only.
    public void ResetSeat(int index)
    {
        if (!IsServer || index < 0 || index >= isSeatOccupied.Count)
        {
            return;
        }
        isSeatOccupied[index] = false;
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
        if (index >= 0 && index < kitchenObjects.Length)
        {
            kitchenObjects[index] = kitchenObject;
        }

    }

    public KitchenObject GetKitchenObject(int index = 0)
    {
        if (index >= 0 && index < kitchenObjects.Length)
        {
            return kitchenObjects[index];
        }
        return null;
    }

    // Called on every peer when the plate at index leaves this table.
    public void ClearKitchenObject(int index = 0)
    {
        if (index < 0 || index >= kitchenObjects.Length)
            return;
        kitchenObjects[index] = null;
        // Taking the plate away frees the seat, as before.
        if (IsServer)
            ResetSeat(index);
    }

    public bool HasKitchenObject(int index = 0)
    {
        return GetKitchenObject(index) != null;
    }
    // Server only: the plate's eaten state replicates to clients by itself.
    public void SetEatenViual(int index, int cash, int exp)
    {
        var tablewareObject = GetKitchenObject(index) as TablewareKitchenObject;

        if(tablewareObject != null)
        {
            tablewareObject.SetEaten(cash, exp);
        }
    }
    public void DestroySelf()
    {
        OnDestroySelf?.Invoke();
        NetworkObject.Despawn(true);
    }

    public bool CanRemove()
    {
        for(int i = 0; i < isSeatOccupied.Count; i++)
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
