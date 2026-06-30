using TMPro;
using UnityEngine;

public class UISavedItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI LastSavedText;

    private SavedData savedData;

    public void Init(SavedData data)
    {
        savedData = data;
        if (savedData != null)
        {
            nameText.text = savedData.GameDataName.ToString();
            LastSavedText.text = savedData.LastSavedTime.ToString();
        }
    }
}