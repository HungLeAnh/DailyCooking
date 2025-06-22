using UnityEngine;
using TMPro;

public class UIGameOverPopup : UIPopup
{
    [SerializeField] private TextMeshProUGUI _dayText;
    [SerializeField] private TextMeshProUGUI _earnText;
    [SerializeField] private TextMeshProUGUI _serveText;

    public override void HidePopup(object param = null)
    {
        base.HidePopup(param);
        Hide();
    }
    public override void ShowPopup(object param = null)
    {
        base.ShowPopup(param);
        Show();
    }
    private void KitchenGameManager_OnStateChanged()
    {
        if (KitchenGameManager.Instance.IsGameOver())
        {
            if(KitchenGameManager.Instance.IsTaskComplete())
                _dayText.text = "Complete";
            else
                _dayText.text = "Fail";

            _serveText.text = KitchenGameManager.Instance.ServeCount.ToString();
            _earnText.text = KitchenGameManager.Instance.EarnCount.ToString();  

        }
    }
    public void OnClick()
    {
        Hide();
        KitchenGameManager.Instance.EndGame();
        UIHUDManager.Instance.ShowAllUIElement();
    }
    private void Hide()
    {
        gameObject.SetActive(false);
    }
    private void Show()
    {
        gameObject.SetActive(true);
        KitchenGameManager_OnStateChanged();
    }
}
