using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIGamePausePopup : UIPopup
{

    [SerializeField] private Button resumeButton;
    [SerializeField] private Button mainMenuButton;

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
        base.ShowPopup();
    }

    private void Hide()
    {
        base.HidePopup();
    }
    public void OnResumeClick()
    {
        KitchenGameManager.Instance.TogglePauseGame();
        HidePopup();
    }
    public void OnMainMenuClick()
    {
        HidePopup();
        KitchenGameManager.Instance.TogglePauseGame();
        KitchenGameManager.Instance.ChangeState(KitchenGameManager.State.Editing);
        KitchenGameManager.Instance.EndGame();
        UIHUDManager.Instance.ShowAllUIElement();
    }
}
