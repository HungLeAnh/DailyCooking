using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum UIHUDElements
{
    Settings,
    Pause,
    Shop,
    Inventory,
    Play,
    Coin,
    Level,
}
[Serializable]
struct SerializableKeyValuePair<T1, T2>
{
    public T1 Key;
    public T2 Value;
    public SerializableKeyValuePair(T1 key, T2 value)
    {
        Key = key;
        Value = value;
    }
}
public class UIHUDManager : PersistentSingleton<UIHUDManager>
{
    [Header("HUD Elements")]
    [SerializeField] private List<SerializableKeyValuePair<UIHUDElements, GameObject>> uiHUDElementList = new List<SerializableKeyValuePair<UIHUDElements, GameObject>>();
    private Dictionary<UIHUDElements, GameObject> uiHUDElementDictionary = new Dictionary<UIHUDElements, GameObject>();

    protected override void Awake()
    {
        foreach (var item in uiHUDElementList)
        {
            uiHUDElementDictionary.TryAdd(item.Key, item.Value);
        }
    }

    public void ShowElement(UIHUDElements element)
    {
        if (uiHUDElementDictionary.TryGetValue(element, out GameObject value))
        {
            value.SetActive(true);
        }
    }    
    public void HideElement(UIHUDElements element)
    {
        if (uiHUDElementDictionary.TryGetValue(element, out GameObject value))
        {
            value.SetActive(false);
        }
    }
    #region Click
    public void OnSettingsClicked()
    {
        UIPopupManager.Instance.ShowPopup(UIPopupType.UISettingPopup);
    }
    public void OnPauseClicked()
    {

    }
    public void OnShopClicked()
    {
        UIPopupManager.Instance.ShowPopup(UIPopupType.UIShopPopup);
    }
    public void OnInventoryClicked()
    {
        UIPopupManager.Instance.ShowPopup(UIPopupType.UIInventoryPopup);
    }
    public void OnPlayClicked()
    {
        HideAllUIElement();
        KitchenGameManager.Instance.StartGame();

    }

    public void HideAllUIElement()
    {
        foreach (var item in uiHUDElementDictionary)
        {
            item.Value.SetActive(false);
        }
    }
    public void ShowAllUIElement()
    {
        foreach (var item in uiHUDElementDictionary)
        {
            item.Value.SetActive(true);
        }
    }
    #endregion
}
