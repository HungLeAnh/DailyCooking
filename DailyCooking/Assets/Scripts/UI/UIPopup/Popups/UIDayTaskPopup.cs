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

    public override void ShowPopup()
    {
        base.ShowPopup();
        Show();
    }
    public void OnCick()
    {
        Hide();
        KitchenGameManager.Instance.ChangeState(KitchenGameManager.State.CountdownToStart);
        UIPopupManager.Instance.ShowPopup(UIPopupType.UIGameStartCountdownPopup.ToString());

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