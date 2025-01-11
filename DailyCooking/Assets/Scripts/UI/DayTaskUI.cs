using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
public class DayTaskUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _dayText;
    [SerializeField] private TextMeshProUGUI _earnGoalText;
    [SerializeField] private TextMeshProUGUI _serveGoalText;

    private void Start()
    {
        KitchenGameManager.Instance.OnStateChanged += KitchenGameManager_OnStateChanged;
    }

    private void KitchenGameManager_OnGameInit(object sender, System.EventArgs e)
    {
        Show();

    }

    private void KitchenGameManager_OnStateChanged(object sender, System.EventArgs e)
    {
        if (KitchenGameManager.Instance.IsCountdownToStartActive())
        {
            Hide();
        }
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