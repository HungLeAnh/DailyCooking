using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class OptionMenuItemUI : MonoBehaviour
{
    public event Action<KitchenObjectSO> OnSelectedOption;

    [SerializeField] private TextMeshProUGUI _kitchenObjectName;
    [SerializeField] private Image _kitchenobjectIcon;
    private KitchenObjectSO _kitchenObjectSO;
    public void Setup(KitchenObjectSO kitchenObjectSO)
    {
        _kitchenObjectSO = kitchenObjectSO;
        _kitchenObjectName.text = kitchenObjectSO.objectName;
        _kitchenobjectIcon.sprite = kitchenObjectSO.Sprite;
    }
    public void OnSelectItem()
    {
       OnSelectedOption?.Invoke(_kitchenObjectSO);
    }
}
