using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIGameNotiPopup : UIPopup
{
    public class Param
    {
        public string Title;
        public string Message;
    }

    [SerializeField] private TextMeshProUGUI notiText;
    [SerializeField] private TextMeshProUGUI titleText;
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
        }
        else
        {
            titleText.text = "Notification";
            notiText.text = "No message provided.";
        }
    }

    private void Hide()
    {
    }
    public void OnCloseButtonClicked()
    {
        HidePopup();
    }
    
}
