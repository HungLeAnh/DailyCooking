using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OptionMenuItemUI : MonoBehaviour
{
    public event Action<int> OnSelectedOption;

    [SerializeField] private TextMeshProUGUI _kitchenObjectName;
    [SerializeField] private Image _kitchenobjectIcon;
    private int _index;
    public void Setup(int index,KitchenObjectSO kitchenObjectSO)
    {
        _index = index;
        _kitchenObjectName.text = kitchenObjectSO.objectName;
        _kitchenobjectIcon.sprite = kitchenObjectSO.Sprite;
    }    
    public void Setup(int index, FoodSO foodSO)
    {
        _index = index;
        _kitchenObjectName.text = foodSO.recipeName;
        _kitchenobjectIcon.sprite = foodSO.Sprite;
    }
    public void OnSelectItem()
    {
       OnSelectedOption?.Invoke(_index);
    }
}
