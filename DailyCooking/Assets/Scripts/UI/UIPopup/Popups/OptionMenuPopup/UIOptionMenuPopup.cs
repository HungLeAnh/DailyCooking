using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UIOptionMenuPopup : UIPopup
{
    public class Param
    {
        public object sender;
        public List<KitchenObjectSO> optionalList;
        public KitchenObjectSO objectSO;
        public string Title;
    }

    [SerializeField] private Transform _menuContainer;
    [SerializeField] private GameObject _itemPrefab;
    [SerializeField] private TextMeshProUGUI _title;

    private IHasOptionalSO _optionalCounter;

    List<OptionMenuItemUI> _menuItems = new List<OptionMenuItemUI>();

    private void Start()
    {
        _itemPrefab.SetActive(false);
    }
    public override void ShowPopup(object param = null)
    {
        base.ShowPopup(param);
        // Opened again while still showing: start from an empty list.
        ClearItems();
        var inputParam = _openParam as Param;
        if (inputParam != null)
        {
            if (inputParam.objectSO != null)
            {
                BaseCounter_OnAnyObjectPlacedHere(inputParam.sender, inputParam.objectSO);
            }
            else if (inputParam.optionalList != null && inputParam.optionalList.Count > 0)
            {
                BaseCounter_OnShowOptionalMenu(inputParam.sender ,inputParam.optionalList);
            }
        }
        else
        {
            HidePopup();
        }
    }
    public override void HidePopup(object param = null)
    {
        base.HidePopup(param);
        Hide();
    }
    private void BaseCounter_OnShowOptionalMenu(object sender, List<KitchenObjectSO> kitchenObjectSOList)
    {
        Show();
        var inputParam = _openParam as Param;
        _title.text = inputParam.Title;
        _optionalCounter = sender as IHasOptionalSO;
        if (_optionalCounter == null)
            return;

        if (kitchenObjectSOList == null)
            return;

        for (int i = 0; i < kitchenObjectSOList.Count; i++)
        {
            var menuItem = Instantiate(_itemPrefab, _menuContainer).GetComponent<OptionMenuItemUI>();
            menuItem.gameObject.SetActive(true);
            menuItem.Setup(i, kitchenObjectSOList[i]);
            menuItem.OnSelectedOption += MenuItem_OnSelectedFood;
            _menuItems.Add(menuItem);
        }
    }

    private void BaseCounter_OnAnyObjectPlacedHere(object sender, KitchenObjectSO kitchenObjectSO)
    {
        Show();
        var inputParam = _openParam as Param;
        _title.text = inputParam.Title;
        _optionalCounter = sender as IHasOptionalSO;
        if (_optionalCounter == null )
            return;

        var processSO = kitchenObjectSO.processSO;
        if (processSO == null)
            return;

        for(int i= 0; i < processSO.processListOutput.Count; i++)
        {
            var menuItem = Instantiate(_itemPrefab, _menuContainer).GetComponent<OptionMenuItemUI>();
            menuItem.gameObject.SetActive(true);
            menuItem.Setup(i, processSO.processListOutput[i]);
            menuItem.OnSelectedOption += MenuItem_OnSelectedOption;
            _menuItems.Add(menuItem);
        }
        
    }


    private void MenuItem_OnSelectedOption(int kitchenObjectIndex)
    {
        SendChoice(kitchenObjectIndex);
    }


    private void MenuItem_OnSelectedFood(int foodindex)
    {
        SendChoice(foodindex);
    }

    // The server applies the choice for the local player.
    private void SendChoice(int index)
    {
        if (_optionalCounter != null && UIManager.Instance != null)
            UIManager.Instance.RequestOption(_optionalCounter, index);
        // Through HidePopup so the manager drops it from the visible list too.
        HidePopup();
    }

    private void Show()
    {
        gameObject.SetActive(true);
        //PlayerStateMachine.Instance.DisableInput(true);
    }
    private void Hide()
    {
        gameObject.SetActive(false);
        _optionalCounter = null;
        ClearItems();
        //PlayerStateMachine.Instance.DisableInput(false);

    }
    private void ClearItems()
    {
        foreach (var item in _menuItems.ToList())
        {
            item.OnSelectedOption -= MenuItem_OnSelectedOption;
            item.OnSelectedOption -= MenuItem_OnSelectedFood;
            Destroy(item.gameObject);
        }
        _menuItems.Clear();
    }
    public void OnClickBackground()
    {
        HidePopup();
    }
}
