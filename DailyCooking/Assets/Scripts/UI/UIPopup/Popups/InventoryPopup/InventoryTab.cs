using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class InventoryTab
{
    [SerializeField] public Sprite _tabIcon = default;
    [SerializeField] public InventoryTabType _tabType = default;

    public Sprite TabIcon => _tabIcon;
    public InventoryTabType TabType => _tabType;
}
