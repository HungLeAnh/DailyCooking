using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class UIPopupManager : PersistentSingleton<UIPopupManager>, IUIPopupManager
{

    [SerializeField] private PopupDatabase popupDatabase;
    [SerializeField] private RectTransform popupContainer;

    private Dictionary<UIPopupType,UIPopup> uiPopupDictionary = new Dictionary<UIPopupType,UIPopup>();
    private List<UIPopup> visiblePopupList = new List<UIPopup>();
    protected override void Awake()
    {
        base.Awake();
        popupDatabase?.Initialize();

    }
    public void HidePopup(UIPopupType popupType,object param = null)
    {
        if (!uiPopupDictionary.TryGetValue(popupType, out var popup))
        {
            Debug.LogWarning($"Popup '{popupType}' not found in the dictionary.");
            return;
        }
        popup.HidePopup(param);
    }

    public void RemoveFromeVisibleList(UIPopup value)
    {
        visiblePopupList.Remove(value);
    }

    public void ShowPopup(UIPopupType popupType,object param = null)
    {
        if (!uiPopupDictionary.TryGetValue(popupType, out var popup))
        {
            CreatePopup(popupType);
            uiPopupDictionary.TryGetValue(popupType, out popup);
        }
        if (popup == null)
        {
            Debug.LogWarning($"Popup '{popupType}' not found in the dictionary.");
            return;
        }
        popup.ShowPopup(param);
        // Keep the list in display order so GetTopShownUIPopup returns the front popup.
        visiblePopupList.Remove(popup);
        visiblePopupList.Add(popup);
    }

    public void CreatePopup(UIPopupType popupType)
    {
        var popupData = popupDatabase.GetPopup(popupType);
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

    public bool IsShowingPopup()
    {
        return visiblePopupList.Count > 0;
    }
}