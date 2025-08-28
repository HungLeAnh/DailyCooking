using UnityEngine;

public class MainMenuState : GameManagerBaseState
{
    public MainMenuState(GameManager gameManager) : base(gameManager) { }

    public override void Enter()
    {
        UIPopupManager.Instance.ShowPopup(UIPopupType.UIMainMenuPopup);
    }
}
