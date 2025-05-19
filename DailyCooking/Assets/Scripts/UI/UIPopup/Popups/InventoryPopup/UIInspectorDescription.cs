using TMPro;
using UnityEngine;

public class UIInspectorDescription : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _textDescription = default;
    [SerializeField] private TextMeshProUGUI _textName = default;

    public void FillDescription(InventoryItem itemToInspect)
    {
        _textName.text = itemToInspect.Name;
        _textDescription.text = itemToInspect.Description;

        _textName.gameObject.SetActive(true);
        _textDescription.gameObject.SetActive(true);
    }
}
