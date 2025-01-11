using UnityEngine;
using TMPro;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _dayText;
    [SerializeField] private TextMeshProUGUI _earnText;
    [SerializeField] private TextMeshProUGUI _serveText;
    private void Start()
    {
        KitchenGameManager.Instance.OnStateChanged += KitchenGameManager_OnStateChanged;
        Hide();
    }

    private void KitchenGameManager_OnStateChanged(object sender, System.EventArgs e)
    {
        if (KitchenGameManager.Instance.IsGameOver())
        {
            Show();
            if(KitchenGameManager.Instance.IsTaskComplete())
                _dayText.text = "Complete";
            else
                _dayText.text = "Fail";

            _serveText.text = KitchenGameManager.Instance.ServeCount.ToString();
            _earnText.text = KitchenGameManager.Instance.EarnCount.ToString();  

        }
        else
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
