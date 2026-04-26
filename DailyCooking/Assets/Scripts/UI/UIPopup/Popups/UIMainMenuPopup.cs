using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIMainMenuPopup : UIPopup
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button startHostingButton;
    [SerializeField] private Button joinButton;

    private void Awake()
    {
        playButton.onClick.AddListener(() =>
        {
            MultiplayerManager.Instance.StartSinglePlayer();
            Loader.Load(Loader.Scene.GameScene);
            GameManager.Instance.SwitchState(new InGameState(GameManager.Instance));
            UIPopupManager.Instance.HidePopup(UIPopupType.UIMainMenuPopup);
            GameManager.Instance.LoadGame();
        });
        startHostingButton.onClick.AddListener(() =>
        {
            MultiplayerManager.Instance.StartHostSession();
            Loader.LoadNetwork(Loader.Scene.GameScene);
            GameManager.Instance.SwitchState(new InGameState(GameManager.Instance));
            UIPopupManager.Instance.HidePopup(UIPopupType.UIMainMenuPopup);
            GameManager.Instance.LoadGame();
        });
        joinButton.onClick.AddListener(() =>
        {
            MultiplayerManager.Instance.StartClientSession();
            GameManager.Instance.SwitchState(new InGameState(GameManager.Instance));
            UIPopupManager.Instance.HidePopup(UIPopupType.UIMainMenuPopup);
        });
        optionsButton.onClick.AddListener(() => {
            UIPopupManager.Instance.ShowPopup(UIPopupType.UISettingPopup);
        });
        quitButton.onClick.AddListener(() =>
        {
            Application.Quit();
        });

    }
    public override void HidePopup(object param = null)
    {
        base.HidePopup(param);
    }
    public override void ShowPopup(object param = null)
    {
        base.ShowPopup(param);
    }
    public override void SetupPopup()
    {
        base.SetupPopup();
    }
}