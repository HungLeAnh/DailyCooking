using UnityEngine;

public class UIInventoryInspector : MonoBehaviour
{
    [SerializeField] private UIInspectorDescription _inspectorDescription = default;

    public void FillInspector(InventoryItem itemToInspect, bool[] availabilityArray = null)
    {

        _inspectorDescription.FillDescription(itemToInspect);
    }
}
