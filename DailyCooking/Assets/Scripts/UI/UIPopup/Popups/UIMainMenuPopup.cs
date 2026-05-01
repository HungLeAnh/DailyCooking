using System;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.UI;

public class UIMainMenuPopup : UIPopup
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button startHostingButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private Button loginButton;

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
        loginButton.onClick.AddListener(() => {
            UIPopupManager.Instance.ShowPopup(UIPopupType.UILoginPopup);
        });

    }
    public override void HidePopup(object param = null)
    {
        base.HidePopup(param);
    }
    public override void ShowPopup(object param = null)
    {
        base.ShowPopup(param);
        if (!AuthenticationService.Instance.IsSignedIn)
        {
            
        }
        else
        {
            
        }
    }
    public override void SetupPopup()
    {
        base.SetupPopup();
    }
}