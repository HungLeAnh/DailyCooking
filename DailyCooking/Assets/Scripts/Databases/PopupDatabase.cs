using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "PopupSystem/PopupDatabase", fileName = "PopupDatabase")]
public class PopupDatabase : ScriptableObject
{
    [SerializeField] private List<PopupData> popups;

    public List<PopupData> Popups { get => popups; set => popups = value; }

    private Dictionary<UIPopupType, PopupData> _popupDict;

    public void Initialize()
    {
        _popupDict = new Dictionary<UIPopupType, PopupData>();
        foreach (var popup in popups)
        {
            if (Enum.TryParse<UIPopupType>(popup.popupName, out var type))
            {
                _popupDict[type] = popup;
            }
        }
    }

    public PopupData GetPopup(UIPopupType popupType)
    {
        if (_popupDict == null) Initialize();
        _popupDict.TryGetValue(popupType, out var popup);
        return popup;
    }

}
