using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIMenuCategoryItem : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI categoryNameText;
    [SerializeField] private Image categoryBGImage;
    [SerializeField] private Button categoryButton;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color selectedColor = Color.yellow;
    public Button CategoryButton { get => categoryButton; }
    private FoodType categoryType;
    public void OnCategoryItemClicked(FoodType type)
    {
        if(type == categoryType)
        {
            SetSelected(true);
        }
        else
        {
            SetSelected(false);
        }
    }

    public void SetCategory(FoodType category)
    {
        categoryNameText.text = category.ToString();
        categoryType = category;
    }
    public void SetSelected(bool isSelected)
    {
        categoryBGImage.color = isSelected ? selectedColor : normalColor;
        categoryNameText.color = isSelected ? Color.white : Color.black;
    }
}