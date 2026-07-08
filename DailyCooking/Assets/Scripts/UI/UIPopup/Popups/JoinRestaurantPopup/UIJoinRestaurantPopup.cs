using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class UIJoinRestaurantPopup : UIPopup
{
    public class Param
    {
        public Action<string, string> OnSubmit { get; set; }
    }

    [SerializeField] private Button loadButton;
    [SerializeField] private Button deleteButton;
    [SerializeField] private GameObject SavedItemPrefab;
    [SerializeField] private Transform SavedItemParent;

    private Action<string, string> callback;
    private int selectedIndex = -1;

    public Button OkButton { get => loadButton; }
    public Button DeleteButton { get => deleteButton; }
    public override void SetupPopup()
    {
        base.SetupPopup();
    }

    private void OnDestroy()
    {

    }

    public override void ShowPopup(object param = null)
    {
        base.ShowPopup(param);
        var inputParam = _openParam as Param;
        if (inputParam != null)
        {
            if(inputParam.OnSubmit != null)
                callback = inputParam.OnSubmit;
        }
        SavedItemPrefab.gameObject.SetActive(false);
        foreach (Transform child in SavedItemParent)
        {
            if (child == SavedItemPrefab.transform) continue;
            Destroy(child.gameObject);
        }
        
        for(var i = 0; i < GameManager.Instance.SavedDataList.Count; i++)
        {
            var item = GameManager.Instance.SavedDataList[i];
            int localIndex = i;
            var instance = Instantiate(SavedItemPrefab, SavedItemParent);
            instance.GetComponent<UISavedItem>().Init(item,()=>
            {
                SelectItem(localIndex);
            });
            Debug.Log("Created Item index: " + i);
            instance.gameObject.SetActive(true);
        }

        OkButton.onClick.RemoveAllListeners();
        OkButton.onClick.AddListener(OnLoad);
        DeleteButton.onClick.RemoveAllListeners();
        DeleteButton.onClick.AddListener(DeleteSaved);
    }

    private void SelectItem(int index)
    {
        Debug.Log("Selected Item: " + index);
        selectedIndex = index;
    }

    public override void HidePopup(object param = null)
    {
        base.HidePopup(param);
        selectedIndex = -1;
    }

    private void OnLoad()
    {
        if (selectedIndex >= 0 && selectedIndex < GameManager.Instance.SavedDataList.Count)
        {
            var selectedData = GameManager.Instance.SavedDataList[selectedIndex];
            callback?.Invoke(selectedData.GameDataName, selectedData.Password);
            Hide();
        }
        else
        {
            UIManager.Instance.ShowAlertMessage("Invalid Restaurant!");
        }
    }
    private void DeleteSaved()
    {
        if (selectedIndex >= 0 && selectedIndex < GameManager.Instance.SavedDataList.Count)
        {
            var savedData = GameManager.Instance.SavedDataList[selectedIndex];
            UIPopupManager.Instance.ShowPopup(UIPopupType.UIGameConfirmPopup, new UIGameConfirmPopup.Param
            {
                Title = "Confirm Delete Saved",
                Message = $"Are you sure you want to delete {savedData.GameDataName} saved ?",
                YesAction = () =>
                {
                    GameManager.Instance.DeleteSavedData(savedData);
                    ShowPopup();
                    selectedIndex = -1;
                }
            });
        }
        else
        {
            UIManager.Instance.ShowAlertMessage("Invalid Restaurant!");
        }
    }
    public void Hide()
    {
        HidePopup();
    }


}
