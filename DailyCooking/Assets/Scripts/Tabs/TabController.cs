using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.Events;
using TMPro;

[System.Serializable]
public class TabPair
{
    public string name; 
    public Button tabButton;
    public GameObject contentPanel;
    public TextMeshProUGUI tabLabel;

    public void SetUpTab(bool isActive, Color activeColor,Color inactiveColor)
    {
        if(tabLabel != null)
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
}

public class TabController : MonoBehaviour
{
    [Header("Configuration")]
    [Tooltip("The index of the tab to open on Start")]
    public int defaultTabIndex = 0;

    [Header("Visuals")]
    public Color activeTabColor = Color.white;
    public Color inactiveTabColor = new Color(0.7f, 0.7f, 0.7f, 1f);

    [Header("Tabs Data")]
    public List<TabPair> tabs = new List<TabPair>();

    [Header("Events")]
    public UnityEvent<int> onTabChanged;

    private void Start()
    {
        for (int i = 0; i < tabs.Count; i++)
        {
            int index = i; 
            tabs[i].tabButton.onClick.AddListener(() => SelectTab(index));
        }

        SelectTab(defaultTabIndex);
    }

    public void SelectTab(int index)
    {
        if (index < 0 || index >= tabs.Count) return;

        for (int i = 0; i < tabs.Count; i++)
        {
            bool isActive = (i == index);

            tabs[i].SetUpTab(isActive, activeTabColor, inactiveTabColor);
        }
        onTabChanged?.Invoke(index);
    }
}