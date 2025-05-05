using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class UIPopupManager : PersistentSingleton<UIPopupManager>
{

    [SerializeField] private PopupDatabase popupDatabase;
    [SerializeField] private RectTransform popupContainer;

    private Dictionary<string,UIPopup> uiPopupDictionary = new Dictionary<string,UIPopup>();
    private List<UIPopup> visiblePopupList = new List<UIPopup>();

    public void HidePopup(string popupName)
    {
        if (!uiPopupDictionary.ContainsKey(popupName))
        {
            Debug.LogWarning($"Popup '{popupName}' not found in the dictionary.");
            return;
        }
        HideUIPopup(popupName);
    }

    private void HideUIPopup(string popupName)
    {
        if(uiPopupDictionary.TryGetValue(popupName, out var value))
        {
            value.HidePopup();
            if(visiblePopupList.Contains(value))
                visiblePopupList.Remove(value);
        }
        else
        {
            Debug.LogWarning($"Popup '{popupName}' not found in the dictionary.");
        }
    }

    public void ShowPopup(string popupName)
    {
        if (!uiPopupDictionary.ContainsKey(popupName))
        {
            CreatePopup(popupName);
        }

        ShowUIPopup(popupName);
    }

    private void ShowUIPopup(string popupName)
    {
        if(uiPopupDictionary.TryGetValue(popupName, out var value))
        {
            value.ShowPopup();
            if (!visiblePopupList.Contains(value))
                visiblePopupList.Add(value);
            
        }
        else
        {
            Debug.LogWarning($"Popup '{popupName}' not found in the dictionary.");
        }
    }

    public void CreatePopup(string name)
    {
        var popupData = popupDatabase.GetPopup(name);
        if(popupData == null)
        {
            Debug.LogWarning($"Popup '{name}' not found in the database.");
            return;
        }
        GameObject createPopupObject = Instantiate(popupData.popupPrefab, popupContainer);
        UIPopup uiPopup = createPopupObject.GetComponent<UIPopup>();
        uiPopup.SetupPopup();
        try
        {
            uiPopupDictionary.Add(name, uiPopup);
        }
        catch (System.Exception)
        {
            Debug.LogWarning($"Popup '{name}' already exists in the dictionary.");
        }
    }
    public UIPopup GetTopShownUIPopup()
    {
        if (visiblePopupList.Count == 0)
            return null;
        return visiblePopupList[visiblePopupList.Count - 1];
    }

}
