using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIGameConfirmPopup : UIPopup
{
    public class Param
    {
        public string Title;
        public string Message;
        public Action YesAction;
        public Action NoAction;
    }

    [SerializeField] private TextMeshProUGUI notiText;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;
    public override void ShowPopup(object param = null)
    {
        base.ShowPopup(param);
        Show();
    }

    public override void HidePopup(object param = null)
    {
        base.HidePopup(param);
        Hide();
    }

    private void Show()
    {
        if(_openParam != null)
        {
            Param notificationParam = _openParam as Param;
            titleText.text = notificationParam.Title;
            notiText.text = notificationParam.Message;
            yesButton.onClick.AddListener(() =>
            {
                notificationParam.YesAction?.Invoke();
                HidePopup();
            });
            noButton.onClick.AddListener(() =>
            {
                notificationParam.NoAction?.Invoke();
                HidePopup();
            });
        }
        else
        {
            titleText.text = "Notification";
            notiText.text = "No message provided.";
        }
    }

    private void Hide()
    {
        yesButton.onClick.RemoveAllListeners();
        noButton.onClick.RemoveAllListeners();
    }
    
}
