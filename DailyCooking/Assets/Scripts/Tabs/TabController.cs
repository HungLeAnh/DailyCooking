using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TabController : MonoBehaviour
{
    public Action onTabChanged;

    [Header("Configuration")]
    [Tooltip("The index of the tab to open on Start")]
    public int defaultTabIndex = 0;

    [Header("Visuals")]
    public Color activeTabColor = Color.white;
    public Color inactiveTabColor = new Color(0.7f, 0.7f, 0.7f, 1f);

    [Header("Tabs Data")]
    public List<TabPair> tabs = new List<TabPair>();


    private TabPair currentTab;
    public TabPair CurrentTab => currentTab;

    private void Start()
    {
        if (tabs.Count == 0||tabs == null)
            return;
        for (int i = 0; i < tabs.Count; i++)
        {
            int index = i; 
            tabs[i].tabButton.onClick.AddListener(() => SelectTab(index));
        }

        SelectTab(defaultTabIndex);
    }

    private void SelectTab(int index)
    {
        if (index < 0 || index >= tabs.Count) return;

        for (int i = 0; i < tabs.Count; i++)
        {
            bool isActive = (i == index);

            tabs[i].SetUpTab(isActive, activeTabColor, inactiveTabColor);
        }
        currentTab = tabs[index];
        onTabChanged?.Invoke();
    }
    public void InitializeTabs(List<string> tabNames, List<Sprite> tabIcons)
    {
        for (int i = 0; i < tabs.Count && i < tabNames.Count && i < tabIcons.Count; i++)
        {
            tabs[i].name = tabNames[i];
            tabs[i].InitTab(tabIcons[i]);
            tabs[i].tabButton.onClick.AddListener(() => SelectTab(i));
        }
        SelectTab(defaultTabIndex);
    }
}