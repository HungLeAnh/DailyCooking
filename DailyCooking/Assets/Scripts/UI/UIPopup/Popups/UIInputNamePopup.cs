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
        public string Placeholder;
        public bool isPassword;
        public Action<string> onValue;
        public Action callback;
    }

    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private Button okButton;
    private Action callback;
    private Action<string> onValue;
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
        onValue = null;
        callback = null;
        inputField.text = string.Empty;
        inputField.contentType = TMP_InputField.ContentType.Standard;
        inputField.ForceLabelUpdate();

        if (_openParam != null)
        {
            Param notificationParam = _openParam as Param;
            titleText.text = notificationParam.Title;
            callback = notificationParam.callback;
            onValue = notificationParam.onValue;
            if (notificationParam.isPassword)
                inputField.contentType = TMP_InputField.ContentType.Password;
            if (notificationParam.Placeholder != null && inputField.placeholder is TMP_Text placeholderText)
                placeholderText.text = notificationParam.Placeholder;
        }
        else
        {
            titleText.text = "Input your restaurant name";
        }
        inputField.ForceLabelUpdate();
    }
    private void OnSubmit()
    {
        if (!StringExtensions.ValidateInput(inputField.text))
        {
            UIManager.Instance.ShowAlertMessage("Invalid Input! Please use only letters and numbers, max length 20 characters.");
            return;
        }

        if (onValue != null)
        {
            string value = inputField.text;
            HidePopup();
            onValue.Invoke(value);
            return;
        }

        GameManager.Instance.UpdateRestaurantNameServerRpc(inputField.text);
        callback?.Invoke();
        HidePopup();
    }

}
