using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TableUpgradeDataList", menuName = "ScriptableObjects/TableUpgradeDataList", order = 1)]
public class TableUpgradeDataList : ScriptableObject
{
    public List<TableUpgradeData> upgradeDataList;
}
