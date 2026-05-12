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
            Hide();
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
        _optionalCounter = (IHasOptionalSO)sender;
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
        _optionalCounter = (IHasOptionalSO)sender;
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
        _optionalCounter.SetOptionKitchenObjectSO(kitchenObjectIndex);
        Hide();
    }


    private void MenuItem_OnSelectedFood(int foodindex)
    {
        _optionalCounter.SetOptionKitchenObjectSO(foodindex);
        Hide();
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
        foreach (var item in _menuItems.ToList())
        {
            item.OnSelectedOption -= MenuItem_OnSelectedOption;
            Destroy(item.gameObject);
        }
        _menuItems.Clear();
        //PlayerStateMachine.Instance.DisableInput(false);

    }
    public void OnClickBackground()
    {
        HidePopup();
    }
}
