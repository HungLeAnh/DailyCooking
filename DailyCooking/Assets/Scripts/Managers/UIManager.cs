
using DG.Tweening;
using TMPro;
using UnityEngine;

public class UIManager : PersistentSingleton<UIManager>
{
    [SerializeField] private UIAlert uiAlert;

    public void ShowAlertMessage(string message)
    {
        uiAlert.StartAlert(message);
    }
}