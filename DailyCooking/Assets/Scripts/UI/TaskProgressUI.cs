using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TaskProgressUI : MonoBehaviour
{
    [SerializeField] private Image _earnProgressBar;
    [SerializeField] private TextMeshProUGUI _earnProgresstext;
    [SerializeField] private Image _serveProgressBar;
    [SerializeField] private TextMeshProUGUI _serveProgresstext;

    private void Start()
    {
        _earnProgressBar.fillAmount = 0;
        _earnProgresstext.text = "0/" + KitchenGameManager.Instance.EarnGoal;        
        _serveProgressBar.fillAmount = 0;
        _serveProgresstext.text = "0/" + KitchenGameManager.Instance.ServeGoal;

        KitchenGameManager.Instance.OnServeFood += KitchenGameManager_OnServeFood;
    }

    private void KitchenGameManager_OnServeFood(object sender, System.EventArgs e)
    {
        _earnProgressBar.fillAmount = KitchenGameManager.Instance.EarnCount / KitchenGameManager.Instance.EarnGoal;
        _earnProgresstext.text = KitchenGameManager.Instance.EarnCount + "/" + KitchenGameManager.Instance.EarnGoal;
        _serveProgressBar.fillAmount = KitchenGameManager.Instance.ServeCount / KitchenGameManager.Instance.ServeGoal;
        _serveProgresstext.text = KitchenGameManager.Instance.ServeCount + "/" + KitchenGameManager.Instance.ServeGoal;

    }
}