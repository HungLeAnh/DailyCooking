using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UINewRestaurantPopup : UIPopup
{
    public class Param
    {
        public Action<string, string> OnSubmit;
    }

    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private TMP_InputField passwordInputField;
    [SerializeField] private Button okButton;
    private Action<string, string> callback;
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
        if(param != null) 
        { 
            var p = param as Param;
            if(p.OnSubmit != null)
            {
                callback = p.OnSubmit;
            }
        }

        okButton.onClick.RemoveAllListeners();
        okButton.onClick.AddListener(OnSubmit);
    }

    public override void HidePopup(object param = null)
    {
        base.HidePopup(param);
    }

    private void OnSubmit()
    {
        if (StringExtensions.ValidateInput(nameInputField.text))
        {
            if (StringExtensions.ValidateInput(passwordInputField.text))
            {
                callback?.Invoke(nameInputField.text, passwordInputField.text);
                HidePopup();
            }
            else
            {
                UIManager.Instance.ShowAlertMessage("Invalid Password! Please use only letters and numbers, max length 20 characters.");
            }
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
