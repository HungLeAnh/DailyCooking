using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class UILoadRestaurantPopup : UIPopup
{
    public class Param
    {
        public Action<string, string> OnSubmit { get; set; }
    }

    [SerializeField] private Button loadButton;
    [SerializeField] private GameObject SavedItemPrefab;
    [SerializeField] private Transform SavedItemParent;

    private Action<string, string> callback;
    private List<UISavedItem> items = new List<UISavedItem>();

    public Button OkButton { get => loadButton; }
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
        foreach(Transform child in SavedItemParent)
        {
            if (child == SavedItemPrefab.transform) continue;
            Destroy(child.gameObject);
        }
        foreach(var item in GameManager.Instance.SavedDataList)
        {
            var instance = Instantiate(SavedItemPrefab, SavedItemParent);
            instance.GetComponent<UISavedItem>().Init(item);
            items.Add(instance.GetComponent<UISavedItem>());
        }

        OkButton.onClick.RemoveAllListeners();
        OkButton.onClick.AddListener(OnLoad);
    }

    public override void HidePopup(object param = null)
    {
        base.HidePopup(param);
    }

    private void OnLoad()
    {
        if (true)
        {

        }
        else
        {
            UIManager.Instance.ShowAlertMessage("Invalid Restaurant Name! Please use only letters and numbers, max length 20 characters.");
        }
    }
    public void Hide()
    {
        HidePopup();
    }


}
