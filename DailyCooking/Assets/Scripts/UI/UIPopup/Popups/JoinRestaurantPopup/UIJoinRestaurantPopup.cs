using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIJoinRestaurantPopup : UIPopup
{
    public class Param
    {
        public Action<string> OnSubmit { get; set; }
    }

    [SerializeField] private Button joinButton;
    [SerializeField] private Button clearButton;
    [SerializeField] private TMP_InputField restaurantCodeInputField;
    private Action<string> callback;

    public Button JoinButton { get => joinButton; }
    public Button ClearButton { get => clearButton; }
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
        
        JoinButton.onClick.RemoveAllListeners();
        JoinButton.onClick.AddListener(OnJoin);
        ClearButton.onClick.RemoveAllListeners();
        ClearButton.onClick.AddListener(ClearInput);
    }

    public override void HidePopup(object param = null)
    {
        base.HidePopup(param);
    }

    private void OnJoin()
    {
        if(restaurantCodeInputField.text.Length > 0)
        {
            callback?.Invoke(restaurantCodeInputField.text);
            HidePopup();
        }
    }
    private void ClearInput()
    {
        restaurantCodeInputField.text = string.Empty;
    }
    public void Hide()
    {
        HidePopup();
    }


}
