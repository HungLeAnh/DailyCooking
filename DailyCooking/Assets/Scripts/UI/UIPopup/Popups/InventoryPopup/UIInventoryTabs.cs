using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UIInventoryTabs : MonoBehaviour
{
    [SerializeField] private GameObject tabPrefab;

    private List<UIInventoryTab> tabList;
    public event UnityAction<InventoryTab> TabChanged;

    bool isSet = false;
    public List<UIInventoryTab> TabList => tabList;
    private void Awake()
    {
        isSet = false;
        tabPrefab.gameObject.SetActive(false);
        
    }
    public void Setup(List<InventoryTab> TabTypesList)
    {
        if(tabList == null)
            tabList = new List<UIInventoryTab>();
        for (int i = 0; i < TabTypesList.Count; i++)
        {
            var tab = Instantiate(tabPrefab, transform);
            tab.gameObject.SetActive(true);
            UIInventoryTab uIInventoryTab = tab.GetComponent<UIInventoryTab>();
            uIInventoryTab.SetTab(TabTypesList[i], false);
            uIInventoryTab.TabClicked += ChangeTab;
            tabList.Add(uIInventoryTab);
        }
        isSet = true;
    }
    public void SetTabs(InventoryTab selectedType)
    {
        for (int i = 0; i < tabList.Count; i++)
        {
            bool isSelected = tabList[i].TabType == selectedType;
            tabList[i].UpdateState(isSelected);
        }

    }

    void ChangeTab(InventoryTab newTabType)
    {
        TabChanged.Invoke(newTabType);
    }
}
