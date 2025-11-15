using System.Collections.Generic;
using UnityEngine;

public class Table : MonoBehaviour,IKitchenObjectParent
{
    [SerializeField] private List<Transform> seats = new List<Transform>();
    [SerializeField] private List<Transform> kitchenObjectFollowPoints = new List<Transform>();
    [SerializeField] private GameObject[] visualGameObjectArray;

    private bool[] isSeatOccupied;
    private KitchenObject[] kitchenObjects;

    public int Level { get; set; } = 1;

    private void Start()
    {
        isSeatOccupied = new bool[seats.Count];
        kitchenObjects = new KitchenObject[seats.Count];
    }

    private void OnEnable()
    {
        TableManager.Instance.RegisterTable(this);
    }

    private void OnDisable()
    {
        TableManager.Instance.UnregisterTable(this);
    }

    public int GetAvailableSeat()
    {
        for (int i = 0; i < seats.Count; i++)
        {
            if (!isSeatOccupied[i])
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

    public void ResetTable()
    {
        for (int i = 0; i < isSeatOccupied.Length; i++)
        {
            isSeatOccupied[i] = false;
        }
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
        if(kitchenObjects != null && index >= 0 && 
            index < kitchenObjects.Length && 
            index < seats.Count)
        {
            kitchenObjects[index] = null;
            isSeatOccupied[index] = false;

        }
    }

    public bool HasKitchenObject(int index = 0)
    {
        if(kitchenObjects != null && 
            kitchenObjects.Length > 0 && 
            index < kitchenObjects.Length)
        {
            return kitchenObjects[index] != null;
        }
        else
        {
            return false;
        }
    }
    public void SetEatenViual(int index)
    {
        var tablewareObject = kitchenObjects[index] as TablewareKitchenObject;

        if(tablewareObject != null)
        {
            tablewareObject.SetEaten();
        }
    }
}
