using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR;

public class BuildingTutorial : TutorialPanel
{
    [SerializeField] private DOTweenHandClick hand;
    private UIShopItem uiShopItem;
    private Vector3 targetPoint;
    protected override void Awake()
    {
        base.Awake();
        previousButton.transform.gameObject.SetActive(false);

    }
    public void OnBuyIngredient()
    {
        var shopButton = UIHUDManager.Instance.GetElementTransform(UIHUDElements.Shop);
        HighlightElement(shopButton as RectTransform, true);
        hightLightButton.onClick.RemoveAllListeners();
        hightLightButton.onClick.AddListener(OnClickHighlightMenu);
    }
    private void OnClickHighlightMenu()
    {
        UIPopupManager.Instance.ShowPopup(UIPopupType.UIShopPopup);
        HighlightElement(transform as RectTransform, false);
        hightLightButton.onClick.RemoveListener(OnClickHighlightMenu);
        NextStep();
    }

    public void OnAddTomatoContainer()
    {
        var popup = UIPopupManager.Instance.GetTopShownUIPopup() as UIShopPopup;
        uiShopItem = popup.GetUIShopItem(ShopItemCategory.Counters, "Tomato Container");
        popup.ScrollTo(ShopItemCategory.Counters, uiShopItem, () =>
        {
            Canvas.ForceUpdateCanvases();
            HighlightElement(uiShopItem.transform as RectTransform, true);
            hightLightButton.onClick.RemoveAllListeners();
            hightLightButton.onClick.AddListener(OnAddedTomatoContainer);
        });

    }

    private void OnAddedTomatoContainer()
    {
        hightLightButton.onClick.RemoveAllListeners();
        uiShopItem.ButtonBuy.onClick?.Invoke();
        var popup = UIPopupManager.Instance.GetTopShownUIPopup() as UIShopPopup;
        uiShopItem = popup.GetUIShopItem(ShopItemCategory.Counters, "Lettuce Container");
        popup.ScrollTo(ShopItemCategory.Counters, uiShopItem, () =>
        {
            Canvas.ForceUpdateCanvases();
            HighlightElement(uiShopItem.transform as RectTransform, true);
            hightLightButton.onClick.RemoveAllListeners();
            hightLightButton.onClick.AddListener(OnAddedLettuceContainer);
        });
    }

    private void OnAddedLettuceContainer()
    {
        hightLightButton.onClick.RemoveAllListeners();
        uiShopItem.ButtonBuy.onClick?.Invoke();
        var popup = UIPopupManager.Instance.GetTopShownUIPopup() as UIShopPopup;

        HighlightElement(popup.CloseButton.transform as RectTransform, true);
        hightLightButton.onClick.RemoveAllListeners();
        hightLightButton.onClick.AddListener(OnCloseButtonClick);

    }
    private void OnCloseButtonClick()
    {
        var popup = UIPopupManager.Instance.GetTopShownUIPopup() as UIShopPopup;
        popup.CloseButton.onClick.Invoke();
        hightLightButton.onClick.RemoveAllListeners();
        GameManager.Instance.GameData.TutorialData.SetHasPlayedFirstTime(true);
        NextStep();
    }
    public void OnGuideClickBuild()
    {
        var buildButton = UIHUDManager.Instance.GetElementTransform(UIHUDElements.Build);
        HighlightElement(buildButton as RectTransform, true);
        hightLightButton.onClick.RemoveAllListeners();
        hightLightButton.onClick.AddListener(OnClickBuild);
    }
    private void OnClickBuild()
    {
        GridBuildingSystem.Instance.StopMoving = true;
        UIPopupManager.Instance.ShowPopup(UIPopupType.UIInventoryPopup);
        HighlightElement(transform as RectTransform, false);
        hightLightButton.onClick.RemoveListener(OnClickBuild);
        OnClickBuildCategory();
    }
    public void OnClickBuildCategory()
    {
        var popup = UIPopupManager.Instance.GetTopShownUIPopup() as UIInventoryPopup;
        var targetItem = popup.InventoryTabs.TabList.Find(x=>x.TabType.TabType == InventoryTabType.Counter);
        HighlightElement(targetItem.transform as RectTransform, true);
        hightLightButton.onClick.RemoveAllListeners();
        hightLightButton.onClick.AddListener(BuildCategoryClicked);
    }

    private void BuildCategoryClicked()
    {
        var popup = UIPopupManager.Instance.GetTopShownUIPopup() as UIInventoryPopup;
        var targetItem = popup.InventoryTabs.TabList.Find(x => x.TabType.TabType == InventoryTabType.Counter);
        targetItem.ClickButton();

        Canvas.ForceUpdateCanvases();
        var invetoryItem = popup.ItemList[0];
        HighlightElement(invetoryItem.transform as RectTransform, true);
        hightLightButton.onClick.RemoveAllListeners();
        hightLightButton.onClick.AddListener(OnClickInventoryItem);
    }

    private void OnClickInventoryItem()
    {
        var popup = UIPopupManager.Instance.GetTopShownUIPopup() as UIInventoryPopup;

        var invetoryItem = popup.ItemList[0];
        invetoryItem.OnItemClick();
        hightLightButton.onClick.RemoveAllListeners();
        HighlightElement(null, false);
        Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0);
        Vector3 screenCenterBottom = new Vector3(Screen.width / 2f, 0, 0);
        hand.gameObject.SetActive(true);
        hand.SetHandDrag(screenCenter, screenCenterBottom);
        GameInput.Instance.OnMouseClickCanceled += OnFirstDrag;
        GridBuildingSystem.Instance.StopMoving = false;
    }

    private void OnFirstDrag(object sender, EventArgs e)
    {
        targetPoint = GridBuildingSystem.Instance.GridManager
            .GridPositionToWorldPosition(new Unity.Mathematics.int2(1, 4));

        hand.gameObject.SetActive(true);
        hand.SetHandDrag3D(BuildingGhost.Instance.transform, targetPoint);
        GameInput.Instance.OnMouseClickCanceled -= OnFirstDrag;

        BuildingGhost.Instance.OnBUildingDrag += OnBuildingDrag;
    }

    private void OnBuildingDrag(Vector3 currentPos)
    {
        currentPos.y = 0;
        if (Vector3.Distance(currentPos,targetPoint) < 0.5f)
        {
            BuildingGhost.Instance.OnBUildingDrag -= OnBuildingDrag;
            BuildingGhost.Instance.IsDragging = false;
            BuildingGhost.Instance.SnapTo(targetPoint);
            BuildingGhost.Instance.StopMoving = true;
            GridBuildingSystem.Instance.StopMoving = true;
            hand.gameObject.SetActive(false);

            HighlightElement(BuildingGhost.Instance.ConfirmButton.transform as RectTransform, true);
            hightLightButton.onClick.RemoveAllListeners();
            hightLightButton.onClick.AddListener(OnPlaceObject);
        }
    }

    private void OnPlaceObject()
    {
        BuildingGhost.Instance.ConfirmButton.onClick?.Invoke();

        NextStep();
    }
    public void OnClickSecondItem()
    {
        var popup = UIPopupManager.Instance.GetTopShownUIPopup() as UIInventoryPopup;

        Canvas.ForceUpdateCanvases();
        var invetoryItem = popup.ItemList[0];
        HighlightElement(invetoryItem.transform as RectTransform, true);
        hightLightButton.onClick.RemoveAllListeners();
        hightLightButton.onClick.AddListener(OnClickInventoryItem2);
    }
    private void OnClickInventoryItem2()
    {
        var popup = UIPopupManager.Instance.GetTopShownUIPopup() as UIInventoryPopup;

        var invetoryItem = popup.ItemList[0];
        invetoryItem.OnItemClick();
        hightLightButton.onClick.RemoveAllListeners();
        HighlightElement(null, false);

        targetPoint = GridBuildingSystem.Instance.GridManager
            .GridPositionToWorldPosition(new Unity.Mathematics.int2(2, 4));

        hand.gameObject.SetActive(true);
        hand.SetHandDrag3D(BuildingGhost.Instance.transform, targetPoint);

        BuildingGhost.Instance.OnBUildingDrag += OnBuildingDrag2;
        BuildingGhost.Instance.StopMoving = false;

    }

    private void OnBuildingDrag2(Vector3 currentPos)
    {
        currentPos.y = 0;
        if (Vector3.Distance(currentPos, targetPoint) < 0.5f)
        {
            BuildingGhost.Instance.OnBUildingDrag -= OnBuildingDrag2;
            BuildingGhost.Instance.IsDragging = false;
            BuildingGhost.Instance.SnapTo(targetPoint);
            BuildingGhost.Instance.StopMoving = true;

            hand.gameObject.SetActive(false);

            HighlightElement(BuildingGhost.Instance.ConfirmButton.transform as RectTransform, true);
            hightLightButton.onClick.RemoveAllListeners();
            hightLightButton.onClick.AddListener(OnPlaceObject2);
        }
    }

    private void OnPlaceObject2()
    {
        BuildingGhost.Instance.ConfirmButton.onClick?.Invoke();

        var popup = UIPopupManager.Instance.GetTopShownUIPopup() as UIInventoryPopup;
        
        HighlightElement(popup.BackButton.transform as RectTransform, true);
        hightLightButton.onClick.RemoveAllListeners();
        hightLightButton.onClick.AddListener(OnBackButtonClick);
    }

    private void OnBackButtonClick()
    {
        var popup = UIPopupManager.Instance.GetTopShownUIPopup() as UIInventoryPopup;
        popup.BackButton.onClick?.Invoke();

        HighlightElement(popup.BackButton.transform as RectTransform, true);
        hightLightButton.onClick.RemoveAllListeners();
        hightLightButton.onClick.AddListener(OnCloseBuildPopup);
    }

    private void OnCloseBuildPopup()
    {
        var popup = UIPopupManager.Instance.GetTopShownUIPopup() as UIInventoryPopup;
        popup.BackButton.onClick?.Invoke();
        BuildingGhost.Instance.StopMoving = false;
        GridBuildingSystem.Instance.StopMoving = false;
        NextStep();
    }
}