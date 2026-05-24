using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class TabPair : MonoBehaviour
{
    public string name; 
    public Button tabButton;
    public GameObject contentPanel;
    public TextMeshProUGUI tabLabel;
    public Image tabIcon;

    public void SetUpTab(bool isActive, Color activeColor,Color inactiveColor)
    {
        if (tabLabel != null)
        {
            tabLabel.text = name;
        }
        if (contentPanel != null)
        {
            contentPanel.SetActive(isActive);
        }

        if (tabButton != null)
        {
            var image = tabButton.GetComponent<Image>();
            if (image != null)
            {
                image.color = isActive ? activeColor : inactiveColor;
            }

            tabButton.interactable = !isActive;
        }
    }
    public void InitTab(Sprite iconSprite)
    {
        if (tabIcon != null)
        {
            tabIcon.sprite = iconSprite;
        }
    }
}
