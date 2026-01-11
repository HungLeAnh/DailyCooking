using System;
using System.Collections;
using UnityEngine;

public class FirstTimeTutorialPanel : TutorialPanel
{
    protected override void Awake()
    {
        base.Awake();

        previousButton.transform.gameObject.SetActive(false);
    }
    public void OnGuideClickMenu()
    {
        var MenuButton = UIHUDManager.Instance.GetElementTransform(UIHUDElements.Menu);
        HighlightElement(MenuButton as RectTransform,true);
        hightLightButton.onClick.RemoveAllListeners();
        hightLightButton.onClick.AddListener(OnClickHighlightMenu);
        
    }
    public void OnFirstShow()
    {
        nextButton.onClick.AddListener(ShowInputNamePopup);

    }

    private void ShowInputNamePopup()
    {
        UIPopupManager.Instance.ShowPopup(UIPopupType.UIInputNamePopup);
        nextButton.onClick.RemoveListener(ShowInputNamePopup);
        var popup = UIPopupManager.Instance.GetTopShownUIPopup() as UIInputNamePopup;
        popup.OkButton.onClick.AddListener(NextStep);
    }
    public void OnUnlockGrid()
    {
        nextButton.onClick.AddListener(OnBuildRestaurant);
    }
    public void OnBuildRestaurant()
    {
        GridBuildingSystem.Instance.UnlockGrid();
        UIHUDManager.Instance.ShowAllUIElement();
        nextButton.onClick.RemoveListener(OnBuildRestaurant);
    }

    private void OnClickHighlightMenu()
    {
        UIPopupManager.Instance.ShowPopup(UIPopupType.UIMenuPopup);
        HighlightElement(transform as RectTransform, false);
        NextStep();
    } 
    public void OnAddDishToMenu()
    {
        var popup = UIPopupManager.Instance.GetTopShownUIPopup() as UIMenuPopup;
        Canvas.ForceUpdateCanvases();
        var uiFoodItem = popup.GetFirstUnlockedFoodItem();
        Canvas.ForceUpdateCanvases();
        HighlightElement(uiFoodItem.transform as RectTransform, true);
        hightLightButton.onClick.RemoveAllListeners();
        hightLightButton.onClick.AddListener(OnAddedDish);
        
    }
    private void OnAddedDish()
    {
        var popup = UIPopupManager.Instance.GetTopShownUIPopup() as UIMenuPopup;
        var uiFoodItem = popup.GetFirstUnlockedFoodItem();
        uiFoodItem.FoodButton.onClick.Invoke();
        HighlightElement(popup.BtnClose.transform as RectTransform, true);
        hightLightButton.onClick.RemoveAllListeners();
        hightLightButton.onClick.AddListener(OnCloseButtonClick);
    }

    private void OnCloseButtonClick()
    {
        var popup = UIPopupManager.Instance.GetTopShownUIPopup() as UIMenuPopup;
        popup.BtnClose.onClick.Invoke();
        hightLightButton.onClick.RemoveAllListeners();
        GameManager.Instance.GameData.TutorialData.SetHasPlayedFirstTime(true);
        NextStep();
    }
}