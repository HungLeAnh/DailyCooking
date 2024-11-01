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

    private BaseCounter _counter;

    List<OptionMenuItemUI> _menuItems = new List<OptionMenuItemUI>();
    private void Start()
    {
        BaseCounter.OnAnyObjectPlacedHere += BaseCounter_OnAnyObjectPlacedHere;
        _itemPrefab.SetActive(false);
        Hide();
    }

    private void BaseCounter_OnAnyObjectPlacedHere(object sender, System.EventArgs e)
    {
        Show();
        _counter = (BaseCounter)sender;
        var processSO = _counter.GetKitchenObject().GetKitchenObjectOptionalProcessSO();
        if (processSO != null)
        {
            foreach (var item in processSO.processListOutput)
            {
                var menuItem = Instantiate(_itemPrefab, _menuContainer).GetComponent<OptionMenuItemUI>();
                menuItem.gameObject.SetActive(true);    
                menuItem.Setup(item);
                menuItem.OnSelectedOption += MenuItem_OnSelectedOption;
                _menuItems.Add(menuItem);
            }
        }
    }


    private void MenuItem_OnSelectedOption(KitchenObjectSO kitchenObjectSO)
    {
        _counter.SetOptionKitchenObjectSO(kitchenObjectSO);
        Hide();
    }


    private void Show()
    {
        gameObject.SetActive(true);
    }
    private void Hide()
    {
        gameObject.SetActive(false);
        foreach (var item in _menuItems.ToList())
        {
            item.OnSelectedOption -= MenuItem_OnSelectedOption;
            Destroy(item.gameObject);
        }
        _menuItems.Clear();
    }
}
