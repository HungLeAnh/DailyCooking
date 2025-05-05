using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIMainMenuPopup : UIPopup
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button optionsButton;
    [SerializeField] private Button quitButton;

    private void Awake()
    {
        playButton.onClick.AddListener(() =>
        {
            Loader.Load(Loader.Scene.GameScene);
            GameManager.Instance.SwitchState(GameState.InGame);
            UIPopupManager.Instance.HidePopup(UIPopupType.UIMainMenuPopup.ToString());
        });
        optionsButton.onClick.AddListener(() => {
            UIPopupManager.Instance.ShowPopup(UIPopupType.UISettingPopup.ToString());
        });
        quitButton.onClick.AddListener(() =>
        {
            Application.Quit();
        });

    }
    
}