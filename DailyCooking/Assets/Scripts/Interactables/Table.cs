using System.Collections.Generic;
using UnityEngine;

// To use the upgrade system, attach a GenericUpgrader component to this GameObject.
// Then, in the Inspector, subscribe the AddSeat method to the OnUpgrade event in the GenericUpgrader.
public class Table : MonoBehaviour, IUpgradeable
{
    public TableUpgradeDataList upgradeDataList;
    public List<Transform> seats = new List<Transform>();
    private bool[] isSeatOccupied;

    public int Level { get; set; } = 1;

    private GenericUpgrader upgrader;

    private void Awake()
    {
        upgrader = GetComponent<GenericUpgrader>();
    }

    private void Start()
    {
        isSeatOccupied = new bool[seats.Count];
    }

    private void OnEnable()
    {
        TableManager.Instance.RegisterTable(this);
        UpgradeManager.Instance.Register(this);
    }

    private void OnDisable()
    {
        TableManager.Instance.UnregisterTable(this);
        UpgradeManager.Instance.Unregister(this);
    }

    public void Upgrade()
    {
        if (Level >= upgradeDataList.upgradeDataList.Count)
        {
            Debug.Log("Max level reached!");
            return;
        }

        TableUpgradeData upgradeData = upgradeDataList.upgradeDataList[Level];

        // TODO: Implement cost handling
        // if (player.money < upgradeData.cost) {
        //     return;
        // }
        // player.money -= upgradeData.cost;

        Level++;
        Debug.Log($"{gameObject.name} upgraded to level {Level}");

        AddSeats(upgradeData.seatsToAdd);

        if (upgradeData.upgradedPrefab != null)
        {
            Instantiate(upgradeData.upgradedPrefab, transform.position, transform.rotation, transform.parent);
            Destroy(gameObject);
        }

        upgrader.Upgrade();
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

    public Transform GetSeatTransform(int seatIndex)
    {
        if (seatIndex >= 0 && seatIndex < seats.Count)
        {
            return seats[seatIndex];
        }
        return null;
    }
}
