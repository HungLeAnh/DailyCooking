using UnityEngine;

[CreateAssetMenu(fileName = "TableUpgradeData", menuName = "ScriptableObjects/TableUpgradeData", order = 1)]
public class TableUpgradeData : ScriptableObject
{
    public int cost;
    public int seatsToAdd;
    public GameObject upgradedPrefab;
}
