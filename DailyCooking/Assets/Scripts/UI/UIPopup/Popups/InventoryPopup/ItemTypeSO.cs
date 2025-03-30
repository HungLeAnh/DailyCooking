using UnityEngine;

[CreateAssetMenu(fileName = "ItemType", menuName = "Inventory/ItemType")]
public class ItemTypeSO : ScriptableObject
{
    [Tooltip("The Item's background color in the UI")]
    [SerializeField] private Color _typeColor = default;
    [Tooltip("The item type")]
    [SerializeField] private ItemInventoryType _type = default;
    [Tooltip("The tab type under which the item will be added")]
    [SerializeField] private InventoryTabType _tabType = default;

    public Color TypeColor => _typeColor;
    public ItemInventoryType Type => _type;
    public InventoryTabType TabType => _tabType;
}
