using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
public class DayTaskUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI _earnGoalText;
    [SerializeField] TextMeshProUGUI _serveGoalText;

    private void Start()
    {
        KitchenGameManager.Instance.OnStateChanged += KitchenGameManager_OnStateChanged;
        _earnGoalText.text = KitchenGameManager.Instance.EarnGoal.ToString();
        _serveGoalText.text = KitchenGameManager.Instance.ServeGoal.ToString();

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
    private void Show()
    {
        gameObject.SetActive(true);

    }
}