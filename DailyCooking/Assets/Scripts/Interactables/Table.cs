using System.Collections.Generic;
using UnityEngine;

public class Table : MonoBehaviour, IInteractable
{
    [SerializeField] private List<Transform> seats = new List<Transform>();
    [SerializeField] private List<Transform> kitchenObjectFollowPoints = new List<Transform>();
    [SerializeField] private GameObject[] visualGameObjectArray;

    private bool[] isSeatOccupied;
    private KitchenObject[] kitchenObjects;

    public int Level { get; set; } = 1;


    private void Awake()
    {
    }

    private void Start()
    {
        isSeatOccupied = new bool[seats.Count];
    }

    private void OnEnable()
    {
        TableManager.Instance.RegisterTable(this);
    }

    private void OnDisable()
    {
        TableManager.Instance.UnregisterTable(this);
    }
    public void AddSeats(int seatsToAdd)
    {
        for (int i = 0; i < seatsToAdd; i++)
        {
            // This is a simple way to add seats, you might want to have a more sophisticated way to determine the position of the new seats
            Vector3 newPosition = seats[seats.Count - 1].position + new Vector3(1.5f, 0, 0);
            GameObject newSeat = new GameObject("Seat");
            newSeat.transform.position = newPosition;
            newSeat.transform.parent = transform;
            seats.Add(newSeat.transform);
        }
        isSeatOccupied = new bool[seats.Count];
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

    public void VacateSeat(int seatIndex)
    {
        if (seatIndex >= 0 && seatIndex < seats.Count)
        {
            isSeatOccupied[seatIndex] = false;
        }
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

    public Transform GetKitchenObjectFollowTransform(int index)
    {
        if (kitchenObjectFollowPoints != null && index >= 0 && index < kitchenObjects.Length)
        {
            return kitchenObjectFollowPoints[index];
        }
        return null;
    }

    public void SetKitchenObject(KitchenObject kitchenObject, int index)
    {
        if (kitchenObjects != null && index >= 0 && index < kitchenObjects.Length)
        {
            kitchenObjects[index] = kitchenObject;
        }
    }

    public KitchenObject GetKitchenObject(int index)
    {
        if (kitchenObjects != null && index >= 0 && index < kitchenObjects.Length)
        {
            return kitchenObjects[index];
        }
        return null;
    }

    public void ClearKitchenObject(int index)
    {
        if(kitchenObjects != null && index >= 0 && index < kitchenObjects.Length)
        {
            kitchenObjects[index] = null;
        }
    }

    public bool HasKitchenObject()
    {
        return kitchenObjects != null && kitchenObjects.Length > 0;
    }

    public void InteractEvent(PlayerStateMachine playerStateMachine)
    {
        Debug.LogError("Table: InteractEvent called");
        if (!playerStateMachine.HasKitchenObject())
        {
            // Player is not carrying anything

        }
        else
        {

        }
    }

    public void InteractAlternateEvent(PlayerStateMachine playerStateMachine)
    {
        
    }

    public void OnSelected()
    {
        Show();
    }

    public void OnDeselected()
    {
        Hide();
    }

    public void Show()
    {
        foreach (var visualGameObject in visualGameObjectArray)
        {
            visualGameObject.SetActive(true);
        }

    }
    public void Hide()
    {
        foreach (var visualGameObject in visualGameObjectArray)
        {
            visualGameObject.SetActive(false);
        }
    }
}
