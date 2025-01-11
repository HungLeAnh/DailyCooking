using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class OptionMenuUI : MonoBehaviour
{
    [SerializeField] private Transform _menuContainer;
    [SerializeField] private GameObject _itemPrefab;
    [SerializeField] private TextMeshProUGUI _title;

    private IHasOptionalSO _optionalCounter;

    List<OptionMenuItemUI> _menuItems = new List<OptionMenuItemUI>();
    private void Start()
    {
        BaseCounter.OnAnyObjectPlacedHere += BaseCounter_OnAnyObjectPlacedHere;
        BaseCounter.OnShowOptionalMenu += BaseCounter_OnShowOptionalMenu;
        _itemPrefab.SetActive(false);
        Hide();
    }
    private void OnDestroy()
    {
        BaseCounter.OnAnyObjectPlacedHere -= BaseCounter_OnAnyObjectPlacedHere;
        BaseCounter.OnShowOptionalMenu -= BaseCounter_OnShowOptionalMenu;
    }
    private void BaseCounter_OnShowOptionalMenu(object sender, BaseCounter.OnShowOptionalMenuArgs e)
    {
        Show();
        _title.text = "Select ingredient to make: ";
        _optionalCounter = (IHasOptionalSO)sender;
        if (_optionalCounter == null)
            return;

        var kitchenObjectSOList = e.optionalList;
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
        _title.text = "Select way to process ingredient:";
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
        PlayerStateMachine.Instance.DisableInput(true);
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
        PlayerStateMachine.Instance.DisableInput(false);

    }
}
