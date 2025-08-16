using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class UIPopupManager : PersistentSingleton<UIPopupManager>
{

    [SerializeField] private PopupDatabase popupDatabase;
    [SerializeField] private RectTransform popupContainer;

    private Dictionary<UIPopupType,UIPopup> uiPopupDictionary = new Dictionary<UIPopupType,UIPopup>();
    private List<UIPopup> visiblePopupList = new List<UIPopup>();

    public void HidePopup(UIPopupType popupType,object param = null)
    {
        if (!uiPopupDictionary.ContainsKey(popupType))
        {
            Debug.LogWarning($"Popup '{popupType}' not found in the dictionary.");
            return;
        }
        HideUIPopup(popupType,param);
    }

    private void HideUIPopup(UIPopupType popupType, object param)
    {
        if(uiPopupDictionary.TryGetValue(popupType, out var value))
        {
            value.HidePopup(param);
            if(visiblePopupList.Contains(value))
                visiblePopupList.Remove(value);
        }
        else
        {
            Debug.LogWarning($"Popup '{popupType}' not found in the dictionary.");
        }
    }

    public void ShowPopup(UIPopupType popupType,object param = null)
    {
        if (!uiPopupDictionary.ContainsKey(popupType))
        {
            CreatePopup(popupType);
        }

        ShowUIPopup(popupType,param);
    }

    private void ShowUIPopup(UIPopupType popupType, object param)
    {
        if(uiPopupDictionary.TryGetValue(popupType, out var value))
        {
            value.ShowPopup(param);
            if (!visiblePopupList.Contains(value))
                visiblePopupList.Add(value);
            
        }
        else
        {
            Debug.LogWarning($"Popup '{popupType}' not found in the dictionary.");
        }
    }

    public void CreatePopup(UIPopupType popupType)
    {
        var popupData = popupDatabase.GetPopup(popupType.ToString());
        if(popupData == null)
        {
            Debug.LogWarning($"Popup '{popupType}' not found in the database.");
            return;
        }
        GameObject createPopupObject = Instantiate(popupData.popupPrefab, popupContainer);
        UIPopup uiPopup = createPopupObject.GetComponent<UIPopup>();
        uiPopup.SetupPopup();
        try
        {
            uiPopupDictionary.Add(popupType, uiPopup);
        }
        catch (System.Exception)
        {
            Debug.LogWarning($"Popup '{popupType}' already exists in the dictionary.");
        }
    }
    public UIPopup GetTopShownUIPopup()
    {
        if (visiblePopupList.Count == 0)
            return null;
        return visiblePopupList[visiblePopupList.Count - 1];
    }

}