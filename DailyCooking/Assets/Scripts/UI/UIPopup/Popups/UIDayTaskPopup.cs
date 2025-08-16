using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
public class UIDayTaskPopup : UIPopup
{
    [SerializeField] private TextMeshProUGUI _dayText;
    [SerializeField] private TextMeshProUGUI _earnGoalText;
    [SerializeField] private TextMeshProUGUI _serveGoalText;

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
    public void OnCick()
    {
        HidePopup();
        KitchenGameManager.Instance.ChangeState(KitchenGameManager.State.CountdownToStart);
        UIPopupManager.Instance.ShowPopup(UIPopupType.UIGameStartCountdownPopup);

    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }
    public void Show()
    {
        gameObject.SetActive(true);
        _earnGoalText.text = KitchenGameManager.Instance.EarnGoal.ToString();
        _serveGoalText.text = KitchenGameManager.Instance.ServeGoal.ToString();
        _dayText.text = "Day " + KitchenGameManager.Instance.PlayerDay;
    }
}