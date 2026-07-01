using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UISavedItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI LastSavedText;
    [SerializeField] private Button selectButton;
    private SavedData savedData;

    public void Init(SavedData data,Action callback)
    {
        savedData = data;
        if (savedData != null)
        {
            nameText.text = savedData.GameDataName.ToString();
            LastSavedText.text = savedData.LastSavedTime.ToString();
        }
        selectButton.onClick.AddListener(() => { 
            callback?.Invoke();
        });
    }
}