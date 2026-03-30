using System;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIInputNamePopup : UIPopup
{
    public class Param
    {
        public string Title;
        public Action callback;
    }

    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button okButton;
    private Action callback;
    public Button OkButton { get => okButton; }
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
        Show();
        okButton.onClick.RemoveAllListeners();
        okButton.onClick.AddListener(OnSubmit);
    }

    public override void HidePopup(object param = null)
    {
        base.HidePopup(param);
    }

    private void Show()
    {
        if(_openParam != null)
        {
            Param notificationParam = _openParam as Param;
            titleText.text = notificationParam.Title;
            callback = notificationParam.callback;
        }
        else
        {
            titleText.text = "Input your restaurant name";
        }
    }
    private void OnSubmit()
    {
        if (StringExtensions.ValidateInput(inputField.text))
        {
            GameManager.Instance.UpdateRestaurantNameServerRpc(inputField.text);
            callback?.Invoke();
            HidePopup();
        }
        else
        {
            UIManager.Instance.ShowAlertMessage("Invalid Input! Please use only letters and numbers, max length 20 characters.");
        }
    }

}
